# RFP Database And Function Summary

## 1) Business Flow

1. Company creates RFP.
2. Buyer invites vendors.
3. Vendor submits bid.
4. Buyer finalizes selected bid.
5. System creates contract.
6. Buyer and vendor sign contract.

Main flow chain:

- Company -> RFP -> RfpItem -> RfpItemSpec
- RFP -> RfpBid -> RfpBidItem -> RfpBidItemSpec
- RFP -> RfpFinalize -> RfpContract

## 2) Core Entity Relationship (High Level)

- Company has one CompanyAddress and many CompanyEmployee.
- Company creates many RFP records.
- RFP belongs to Category and has many RfpItem, RfpAttachment, RfpVendorParticipation, RfpBid.
- RfpItem has many RfpItemSpec.
- RfpBid has many RfpBidItem.
- RfpBidItem has many RfpBidItemSpec.
- RfpFinalize references one RFP and one winning RfpBid.
- RfpFinalize has many RfpFinalizeItem and many RfpVendorConfirmation.
- RfpContract references one RfpFinalize.

## 3) Database Schema

## 3.1 Company

### Company

- PK _id int NOT NULL
- TaxCode string NOT NULL
- FullName string NOT NULL
- ShortName string NOT NULL
- Email string
- PhoneNumber string
- AvatarId string
- Description string NOT NULL
- Type int NOT NULL
- Status int NOT NULL
- AddressId int NOT NULL
- CreateAt datetime NOT NULL
- UpdateAt datetime

### CompanyAddress

- PK _id int NOT NULL
- TenantId string NOT NULL
- UserId objectId NOT NULL
- ProvinceCode string
- DistrictCode string
- WardCode string
- StreetAddress string
- FullAddress string
- CreateAt datetime NOT NULL

### CompanyEmployee

- PK _id int NOT NULL
- CompanyId int NOT NULL
- EmpId string NOT NULL
- FirstName string NOT NULL
- LastName string NOT NULL
- FullName string NOT NULL
- AvatarId string
- Email string
- PhoneNumber string
- Type int NOT NULL
- Status int NOT NULL
- CreateAt datetime NOT NULL
- UpdateAt datetime

## 3.2 Master Data

### AdministrativeUnit

- PK _id int NOT NULL
- TenantId string NOT NULL
- Code string NOT NULL
- Name string NOT NULL
- ParentCode string
- Level int NOT NULL
- CreateAt datetime NOT NULL

### Category

- PK _id objectid NOT NULL
- CategoryCode string NOT NULL
- CategoryName string NOT NULL
- Description string
- IsActive bool NOT NULL
- ParentId objectId
- TreePath string
- CreateAt datetime NOT NULL

## 3.3 RFP Domain

### RFP

- PK _id int NOT NULL
- CompanyId int NOT NULL
- Title string NOT NULL
- Description string NOT NULL
- BudgetMin numeric
- BudgetMax numeric
- CategoryId int NOT NULL
- Note string
- Type int NOT NULL
- PrivacyMode int NOT NULL
- Status int NOT NULL
- Deadline datetime
- CreateAt datetime NOT NULL
- CreateEmpId objectId NOT NULL
- UpdateAt datetime
- UpdateEmpId objectId

### RfpItem

- PK _id int NOT NULL
- RfpId int NOT NULL
- Name string NOT NULL
- Quantity int NOT NULL
- Unit string NOT NULL
- Note string
- Status int NOT NULL
- CreateAt datetime NOT NULL
- CreateEmpId objectId NOT NULL
- UpdateAt datetime
- UpdateEmpId objectId

### RfpItemSpec

- PK _id int NOT NULL
- RfpItemId int NOT NULL
- Key string NOT NULL
- ValueText string
- ValueNumber numeric
- ValueBoolean boolean
- Unit string
- Status int NOT NULL
- CreateAt datetime NOT NULL
- CreateEmpId objectId NOT NULL
- UpdateAt datetime
- UpdateEmpId objectId

### RfpAttachment

- PK _id int NOT NULL
- RfpId int NOT NULL
- FileName string NOT NULL
- FileUrl string NOT NULL
- Status int NOT NULL
- CreateAt datetime NOT NULL
- CreateEmpId objectId NOT NULL
- UpdateAt datetime
- UpdateEmpId objectId

### RfpVendorParticipation

- PK _id int NOT NULL
- RfpId int NOT NULL
- CompanyId int NOT NULL
- Status int NOT NULL
- InviteAt datetime NOT NULL
- ResponseAt datetime NOT NULL

## 3.4 Bid Domain

### RfpBid

- PK _id int NOT NULL
- RfpId int NOT NULL
- CompanyId int NOT NULL
- VatRate decimal NOT NULL
- SubTotal decimal NOT NULL
- GrandTotal decimal NOT NULL
- Currency string NOT NULL
- Proposal string
- PrivacyMode int NOT NULL
- Status int NOT NULL
- CreateAt datetime NOT NULL
- CreateEmpId objectId NOT NULL
- UpdateAt datetime
- UpdateEmpId objectId

### RfpBidItem

- PK _id int NOT NULL
- RfpBidId int NOT NULL
- RfpItemId int NOT NULL
- CompanyId int NOT NULL
- Branch string NOT NULL
- Quantity int NOT NULL
- UnitPrice decimal NOT NULL
- TotalPrice decimal NOT NULL
- Currency string NOT NULL
- Note string
- Status int NOT NULL
- CreateAt datetime NOT NULL
- CreateEmpId objectId NOT NULL
- UpdateAt datetime
- UpdateEmpId objectId

### RfpBidItemSpec

- PK _id int NOT NULL
- RfpBidItemId int NOT NULL
- Key string NOT NULL
- ValueText string
- ValueNumber numeric
- ValueBoolean boolean
- Unit string
- Status int NOT NULL
- CreateAt datetime NOT NULL
- CreateEmpId objectId NOT NULL
- UpdateAt datetime
- UpdateEmpId objectId

## 3.5 Finalize And Contract Domain

### RfpFinalize

- PK _id int NOT NULL
- RfpId int NOT NULL
- RfpBidId int NOT NULL
- CompanyId int NOT NULL
- VatRate decimal NOT NULL
- SubTotal decimal NOT NULL
- GrandTotal decimal NOT NULL
- Currency string NOT NULL
- Note string
- Status int NOT NULL
- CreateAt datetime NOT NULL
- CreateEmpId objectId NOT NULL

### RfpFinalizeItem

- PK _id int NOT NULL
- RfpFinalizeId int NOT NULL
- RfpItemId int NOT NULL
- RfpBidItemId int NOT NULL
- Quantity int NOT NULL
- UnitPrice decimal NOT NULL
- TotalPrice decimal NOT NULL
- Currency string NOT NULL
- Status int NOT NULL
- CreateAt datetime NOT NULL
- CreateEmpId objectId NOT NULL

### RfpVendorConfirmation

- PK _id int NOT NULL
- RfpFinalizeId int NOT NULL
- CompanyId int NOT NULL
- Note string
- Status int NOT NULL
- CreateAt datetime NOT NULL
- CreateEmpId objectId NOT NULL
- UpdateAt datetime
- UpdateEmpId objectId

### RfpContract

- PK _id int NOT NULL
- RfpFinalizeId int NOT NULL
- ContractNo string NOT NULL
- Title string NOT NULL
- FileUrl string NOT NULL
- BuyerSign bool
- BuyerSignAt datetime
- VendorSign bool
- VendorSignAt datetime
- Note string
- Status int NOT NULL
- CreateAt datetime NOT NULL
- CreateEmpId objectId NOT NULL
- UpdateAt datetime
- UpdateEmpId objectId

## 4) Function And API Summary

## 4.1 App

### Company

- POST /api/company (request to Super Admin)

### Bid (Vendor)

- POST /api/rfp-bid
- GET /api/rfp-bid/{id}

## 4.2 Admin

### Company

- ...

### Employee

- POST /api/employee
- GET /api/employees
- GET /api/employee{id}
- PATCH /api/employee

### RFP

- POST /api/rfp
	- Insert RFP
	- Insert Items
	- Insert Specs
- GET /api/rfps
- GET /api/rfp/{id}
- GET /api/rfp/{rfpId}/bid
- GET /api/rfp/{rfpId}/finalize

### RFP Items

- POST /api/rfp/{rfpId}/items (optional)
- PATCH /api/rfp-item/{id}

### Vendor Participation

- POST /api/rfp/{rfpId}/invite

### Bid (Vendor)

- GET /api/rfp-bid/{id}

### Finalize

- POST /api/rfp-finalize

### Contract

- POST /api/contract/{id}/sign
- GET /api/contract/{id}
- Buyer: POST /api/contract
- Vendor: POST /api/contract/{id}/decline

## 5) Example Request (Create RFP)

```json
{
	"title": "Purchase RAM for system",
	"categoryId": 2,
	"items": [
		{
			"name": "DDR4 RAM",
			"quantity": 2,
			"unit": "sticks",
			"specs": [
				{ "key": "Capacity", "valueNumber": 16, "unit": "GB" },
				{ "key": "Bus", "valueNumber": 3200, "unit": "MHz" },
				{ "key": "Type", "valueText": "DDR4" }
			]
		}
	]
}
```

## 6) Notes

- Diagram image confirms major entities and process flow.
- Column-level schema is consolidated from provided draw.io extracted content.
- Function list above reflects currently provided API skeleton and can be expanded to full CRUD + workflow actions.
