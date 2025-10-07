# Multi-Location Architecture Design

## Overview

Based on industry leaders (Birdeye, ReviewTrackers), this design implements a scalable multi-location system supporting:
- **Hierarchical organization structure** (Company → Region → State → City → Location)
- **Location-based access control** (assign users to specific locations)
- **Aggregated & location-specific views** (whole business vs individual locations)
- **Location comparison analytics** (which locations perform best)

## Database Schema

### Core Hierarchy Model

```
Organization (Company)
  └─ LocationGroup (Custom Hierarchy: Region, District, Territory, etc.)
      └─ LocationGroup (Sub-groups)
          └─ Location (Physical location)
```

### Key Entities

#### 1. **Organization** (Top-level entity)
- Represents the entire company (e.g., "Joe's Restaurant Chain")
- One organization per customer account
- Contains global settings and branding

#### 2. **LocationGroup** (Flexible hierarchy)
- Supports custom hierarchy levels (Region > State > City, or Division > Department, etc.)
- Self-referencing for unlimited nesting
- Examples: "West Coast Region", "California", "Los Angeles"

#### 3. **Location** (Physical business location)
- Individual restaurant/store/office
- Has address, phone, hours
- Connected to platform integrations (Google, Yelp, etc.)

#### 4. **UserLocationAccess** (Access control)
- Maps users to locations they can access
- Supports: All Locations, Specific Locations, Location Groups
- Example: Regional manager sees all locations in their region

## Data Model (Entity Relationships)

```
User
  ├─ Organization (belongs to one organization)
  └─ UserLocationAccess[] (can access multiple locations/groups)

Organization
  ├─ LocationGroups[] (has multiple groups)
  ├─ Locations[] (has multiple locations)
  └─ Users[] (has multiple users)

LocationGroup
  ├─ Organization (belongs to)
  ├─ ParentGroup (optional, for nesting)
  ├─ ChildGroups[] (sub-groups)
  └─ Locations[] (locations in this group)

Location
  ├─ Organization (belongs to)
  ├─ LocationGroup (optional parent group)
  ├─ PlatformConnections[] (Google, Yelp, etc.)
  ├─ Reviews[] (all reviews for this location)
  └─ Customers[] (location-specific customers)

Review
  ├─ Location (which location)
  └─ Business (legacy, for backward compatibility)

UserLocationAccess
  ├─ User
  ├─ Location (optional - specific location)
  ├─ LocationGroup (optional - group of locations)
  └─ HasAllLocationsAccess (bool - access to all)
```

## Access Control Patterns

### Pattern 1: All Locations Access
```csharp
UserLocationAccess {
  UserId: 123,
  OrganizationId: 1,
  HasAllLocationsAccess: true,
  LocationId: null,
  LocationGroupId: null
}
```
**Use Case**: CEO, Admin - sees everything

### Pattern 2: Specific Locations
```csharp
UserLocationAccess {
  UserId: 456,
  LocationId: 10,  // Downtown LA location
  HasAllLocationsAccess: false
}
```
**Use Case**: Store manager - sees only their location

### Pattern 3: Location Group Access
```csharp
UserLocationAccess {
  UserId: 789,
  LocationGroupId: 5,  // "California" group
  HasAllLocationsAccess: false
}
```
**Use Case**: Regional manager - sees all California locations

### Pattern 4: Multiple Specific Locations
```csharp
// Multiple records
UserLocationAccess { UserId: 999, LocationId: 10 }
UserLocationAccess { UserId: 999, LocationId: 15 }
UserLocationAccess { UserId: 999, LocationId: 22 }
```
**Use Case**: Multi-location manager - sees 3 specific locations

## API Design

### Location Filtering Middleware

Every API request automatically filters by user's location access:

```csharp
// Middleware extracts user's accessible location IDs
var accessibleLocationIds = await GetUserAccessibleLocationIds(userId);

// Query automatically filters
var reviews = _context.Reviews
    .Where(r => accessibleLocationIds.Contains(r.LocationId))
    .ToList();
```

### Location-Aware Endpoints

```
GET /api/analytics/overview?locationId={id}       // Specific location
GET /api/analytics/overview?locationGroupId={id}  // Group rollup
GET /api/analytics/overview                       // All accessible locations

GET /api/reviews?locationId={id}                  // Location-specific reviews
GET /api/reviews/compare?locationIds=1,2,3        // Compare locations

GET /api/locations                                // User's accessible locations
GET /api/locations/{id}/hierarchy                 // Location's hierarchy path
GET /api/locations/groups                         // Location groups
```

## Dashboard Views

### 1. **Global Dashboard** (All Locations)
```
Total Reviews: 12,547 (across all 45 locations)
Average Rating: 4.6
Top Performing: Downtown LA (4.9★)
Needs Attention: Airport Blvd (3.2★)

[Chart: Reviews by Location]
[Map: Location Performance Heat Map]
```

### 2. **Location-Specific Dashboard**
```
Location: Downtown LA
Reviews: 834
Rating: 4.9★
Response Rate: 95%
[Location-specific charts]
```

### 3. **Comparison View**
```
Compare Locations: [Dropdown multi-select]

|  Location      | Reviews | Rating | Response Rate | Sentiment |
|----------------|---------|--------|---------------|-----------|
| Downtown LA    | 834     | 4.9★   | 95%           | 87% ⬆    |
| Beach Blvd     | 623     | 4.7★   | 88%           | 82% →    |
| Airport Blvd   | 412     | 3.2★   | 45%           | 52% ⬇    |
```

## UI Components

### Location Selector (Header)
```
[Dropdown]
  All Locations (45) ⭐ Default for admins
  ────────────────
  📊 West Coast Region (25)
    └─ California (15)
        └─ Los Angeles (8)
            • Downtown LA
            • Beach Blvd
            • ...
    └─ Oregon (10)
  📊 East Coast Region (20)
```

### Location Management Page
```
/settings/locations

[+ Add Location] [+ Add Group]

Groups:
  📁 West Coast Region
     📁 California
        📍 Downtown LA (4.9★, 834 reviews)
        📍 Beach Blvd (4.7★, 623 reviews)
     📁 Oregon
        📍 Portland Central (4.8★, 512 reviews)

[Edit] [Delete] [Manage Integrations] buttons per location
```

### User Management (Access Control)
```
/settings/team

User: John Doe (Regional Manager)
Access:
  [x] West Coast Region (includes all sub-locations)
  [ ] Specific Locations
      [x] Downtown LA
      [x] Beach Blvd
  [ ] All Locations
```

## Integration Flow

### Platform Connection (Google/Yelp) Per Location

```
1. User selects Location: "Downtown LA"
2. Clicks "Connect Google Business Profile"
3. OAuth flow includes location context
4. PlatformConnection created:
   {
     LocationId: 10,
     Platform: "Google",
     ExternalId: "ChIJxxxx",  // Google Place ID
     ExternalName: "Joe's Restaurant - Downtown LA"
   }
5. Reviews sync to that specific LocationId
```

### Review Import

```csharp
// When importing from Google for Location #10
Review {
  LocationId: 10,
  BusinessId: 1,  // Legacy for backward compatibility
  Platform: "Google",
  ReviewerName: "Sarah",
  Rating: 5,
  ReviewText: "Great service at the downtown location!"
}
```

## Analytics & Reporting

### Location Performance Metrics

```sql
-- Best performing locations
SELECT
  l.Name,
  AVG(r.Rating) as AvgRating,
  COUNT(r.Id) as TotalReviews,
  SUM(CASE WHEN r.ResponseText IS NOT NULL THEN 1 ELSE 0 END) * 100.0 / COUNT(r.Id) as ResponseRate
FROM Locations l
LEFT JOIN Reviews r ON r.LocationId = l.Id
WHERE l.OrganizationId = @orgId
GROUP BY l.Name
ORDER BY AvgRating DESC;
```

### Location Comparison

```sql
-- Compare locations in a group
SELECT
  l.Name as Location,
  COUNT(r.Id) as Reviews,
  AVG(r.Rating) as AvgRating,
  SUM(CASE WHEN r.Sentiment = 'Positive' THEN 1 ELSE 0 END) * 100.0 / COUNT(r.Id) as PositiveSentiment
FROM Locations l
LEFT JOIN Reviews r ON r.LocationId = l.Id
WHERE l.LocationGroupId = @groupId
GROUP BY l.Name;
```

## Migration Strategy

### Phase 1: Add New Tables (Non-breaking)
- Create Organization, LocationGroup, Location, UserLocationAccess tables
- Keep existing Business table for backward compatibility
- Business.Id → Organization.Id (one-to-one initially)

### Phase 2: Data Migration
- Convert existing Business records to Organization + Location
- If business has multiple "businesses", create one Organization with multiple Locations
- Migrate Reviews.BusinessId → Reviews.LocationId

### Phase 3: Update Application
- Add location selector to UI
- Update all queries to filter by LocationId
- Add location management pages

### Phase 4: Deprecate Legacy (Optional)
- After migration complete, Business table can be kept or removed
- Reviews.BusinessId can be deprecated in favor of Reviews.LocationId

## Scalability Considerations

### Database Indexing
```sql
CREATE INDEX IX_Reviews_LocationId ON Reviews(LocationId);
CREATE INDEX IX_Reviews_LocationId_CreatedAt ON Reviews(LocationId, ReviewDate);
CREATE INDEX IX_UserLocationAccess_UserId ON UserLocationAccess(UserId);
CREATE INDEX IX_Locations_OrganizationId ON Locations(OrganizationId);
CREATE INDEX IX_LocationGroups_ParentGroupId ON LocationGroups(ParentGroupId);
```

### Caching Strategy
- Cache user's accessible location IDs (refreshed on permission change)
- Cache location hierarchy tree (refreshed on structure change)
- Cache aggregated metrics per location (refreshed hourly or on new review)

### Query Optimization
- Use CTEs for recursive location group queries
- Materialize location hierarchies for faster lookups
- Partition Reviews table by LocationId for large datasets

## Security Considerations

### Row-Level Security
- Every query MUST filter by user's accessible locations
- Middleware enforces location access on every request
- API never exposes data from inaccessible locations

### Audit Trail
```csharp
LocationAccessLog {
  UserId: who
  LocationId: which location
  Action: "view_reviews", "edit_settings"
  Timestamp: when
}
```

## Example Use Cases

### Use Case 1: Multi-State Restaurant Chain
```
Joe's Restaurants (Organization)
  └─ West Region
      └─ California
          └─ Los Angeles
              • Downtown LA
              • Santa Monica
          └─ San Francisco
              • Union Square
      └─ Nevada
          └─ Las Vegas Strip
  └─ East Region
      └─ New York
          └─ Manhattan
              • Times Square
```

**Users**:
- **CEO**: All Locations access
- **West Regional Manager**: West Region group access
- **LA District Manager**: Los Angeles group access
- **Store Manager**: Downtown LA location only

### Use Case 2: Franchise Model
```
Burger King Corp (Organization - Franchisor view)
  └─ Franchisee A (LocationGroup)
      • Location 1
      • Location 2
  └─ Franchisee B (LocationGroup)
      • Location 3
      • Location 4
```

**Users**:
- **Corporate**: All Locations
- **Franchisee A Owner**: Franchisee A group only
- **Franchisee B Owner**: Franchisee B group only

## Next Steps

1. ✅ Create database entities in Core
2. ✅ Create EF migration
3. ✅ Add location filtering middleware
4. ✅ Update API controllers
5. ✅ Build location management UI
6. ✅ Add location selector to header
7. ✅ Implement comparison analytics
8. ✅ Update all existing pages for multi-location support
