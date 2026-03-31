# Product Requirements Document (PRD)
## Product: Smart Expense Tracker with AI Insights

---

## 1. Product Summary

### 1.1 Overview
Smart Expense Tracker is a web-based financial management application that allows users to:

- Track personal expenses
- Automatically categorize expenses using AI
- View analytics and insights on spending behavior
- Manage financial habits with minimal manual effort

The system combines:
- Structured backend engineering (CRUD, analytics, authentication)
- AI-powered classification and insights
- Scalable API-first architecture for future mobile expansion

### 1.2 Product Vision
Help users understand and improve their spending behavior through a fast, low-friction expense tracking experience enhanced by intelligent categorization and actionable financial insights.

### 1.3 Objectives

#### Primary Goals
- Reduce friction in expense tracking
- Provide useful and trustworthy financial insights
- Build a production-style product that combines backend engineering with practical AI

#### Secondary Goals
- Establish a reusable backend for future mobile applications
- Create a portfolio-grade system that demonstrates product thinking and engineering maturity

### 1.4 Business Value
This product creates value by:
- Turning manual tracking into a lightweight daily action
- Helping users quickly identify where their money is going
- Demonstrating a viable AI-assisted feature with real utility rather than novelty

### 1.5 Success Metrics
| Metric | Target |
|---|---:|
| Expense entry completion rate | > 90% |
| Median time to log an expense | < 5 seconds |
| Percentage of expenses auto-categorized | > 70% |
| User acceptance of AI category suggestions | > 80% |
| Monthly user retention | > 50% |
| Dashboard load time | < 2 seconds |

---

## 2. Product Scope

### 2.1 In Scope for MVP
- User registration, login, logout
- JWT-based authentication
- Expense CRUD
- Predefined categories
- Optional manual category override
- AI-based category suggestion from text
- Monthly analytics dashboard
- Category breakdown analytics
- Basic AI or rule-based financial insights
- Web application
- API-first architecture for future mobile app reuse

### 2.2 Out of Scope for MVP
- Bank account integration
- Receipt OCR
- Shared/family accounts
- Budget goals and notifications
- Recurring expenses
- Export to PDF/CSV
- Multi-currency accounting logic beyond a basic selected currency field
- Advanced financial planning or forecasting

### 2.3 Assumptions
- Users enter expenses manually
- AI is used as an assistive feature, not as a source of truth
- Analytics are generated from stored expense data
- The initial version is optimized for individual users only

---

## 3. Target Users and Personas

### 3.1 Persona A: Student Tracker
**Profile**
- Age: 19–27
- Tech comfort: high
- Income level: limited or variable
- Main devices: phone and laptop

**Goals**
- Quickly log expenses
- Understand where money goes each month
- Avoid manually categorizing everything

**Pain Points**
- Expense tracking feels tedious
- Spending is fragmented across many small purchases
- Hard to spot patterns without summaries

**Needs**
- Very fast entry flow
- Clean dashboard
- Useful monthly breakdowns
- Minimal setup

### 3.2 Persona B: Early Career Professional
**Profile**
- Age: 24–35
- Tech comfort: medium to high
- Wants better financial awareness

**Goals**
- Monitor lifestyle spending
- Understand category trends
- Reduce unnecessary spending

**Pain Points**
- Spending feels higher than expected
- Manual spreadsheets are inconvenient
- Needs quick insights, not raw transaction lists

**Needs**
- Reliable categorization
- Strong monthly summaries
- Clear top-spending categories

### 3.3 Persona C: Data-Curious User
**Profile**
- Interested in dashboards, trends, and metrics

**Goals**
- Explore spending patterns over time
- Review detailed analytics
- Validate financial habits with data

**Pain Points**
- Basic trackers often lack meaningful analytics
- Wants both detail and summary

**Needs**
- Filtering
- Historical views
- More nuanced insights

---

## 4. Problem Statement

Users want to understand and control their spending, but traditional tracking tools often require too much manual categorization and too much repetitive data entry. This leads to poor consistency, incomplete records, and low long-term engagement.

The product addresses this by combining:
- Fast manual expense logging
- AI-assisted category suggestion
- Clear, digestible spending analytics
- Insight generation based on actual user data

---

## 5. Product Principles

1. **Fast first**  
   Logging an expense should take only a few seconds.

2. **AI as assistance, not authority**  
   The user remains in control. AI can suggest, but the system must allow correction.

3. **Trustworthy numbers**  
   All analytics must be deterministic and derived from stored data.

4. **Clear and lightweight UI**  
   The product should feel clean, calm, and immediately usable.

5. **API-first architecture**  
   The backend should serve both the website and a future mobile app.

---

## 6. User Journey

### 6.1 Primary Journey
1. User lands on the site
2. User signs up or logs in
3. User reaches the dashboard
4. User adds an expense
5. System suggests a category automatically
6. User confirms or edits the category
7. Expense is saved
8. Dashboard updates summary cards and charts
9. User reviews insights and trends

### 6.2 Returning User Journey
1. User logs in
2. User views current month summary
3. User adds one or more expenses
4. User filters historical expenses if needed
5. User checks category breakdown and month-over-month changes

---

## 7. Functional Requirements Overview

### 7.1 Core Functional Areas
- Authentication and session management
- Expense management
- Category management
- AI categorization
- Analytics dashboard
- Insight generation
- Error and empty states
- User settings (minimal MVP version)

---

## 8. Feature Requirements

## 8.1 Authentication

### Purpose
Allow users to securely create accounts and access only their own expense data.

### User-Facing Screens
- Sign Up page
- Login page
- Logout action from authenticated session

### Sign Up Screen

#### UI Elements
- Page title: `Create your account`
- Subtitle: `Start tracking your expenses in minutes`
- Input fields:
  - Email
  - Password
  - Confirm Password
- Primary button: `Create Account`
- Secondary link: `Already have an account? Log in`

#### Field Details
| Field | Type | Required | Validation |
|---|---|---:|---|
| Email | Email input | Yes | Must be valid email format |
| Password | Password input | Yes | Minimum 8 characters |
| Confirm Password | Password input | Yes | Must match Password |

#### Success Behavior
- On successful registration, user is either:
  - automatically logged in and redirected to dashboard, or
  - redirected to login page with success message  
  Product decision: auto-login preferred for MVP.

#### Error States
- Invalid email format
- Password too short
- Passwords do not match
- Email already exists
- Generic server error

### Login Screen

#### UI Elements
- Page title: `Welcome back`
- Input fields:
  - Email
  - Password
- Primary button: `Log In`
- Secondary link: `Create account`
- Optional helper text: `Forgot password` (non-functional placeholder allowed only if clearly marked as coming soon)

#### Success Behavior
- User receives authenticated session
- Redirect to dashboard

#### Error States
- Wrong credentials
- Missing required fields
- Generic server error

### Acceptance Criteria
- User can register with a unique email and valid password
- Passwords are stored hashed, never in plaintext
- User can log in with correct credentials
- JWT token is issued after successful login
- Protected routes reject unauthenticated requests
- Authenticated users can only access their own data

---

## 8.2 Dashboard Home

### Purpose
Provide a clear top-level overview of the user’s spending for the selected period.

### Screen Layout
The dashboard should contain the following zones:
1. Top navigation
2. Period selector
3. Summary cards
4. Insights section
5. Charts section
6. Recent expenses list or shortcut
7. Add expense CTA

### Top Navigation
- Logo / product name
- Navigation items:
  - Dashboard
  - Expenses
  - Analytics
  - Profile or Account
- Logout button or profile dropdown

### Period Selector
- Default period: current month
- Control type: dropdown or month picker
- Options:
  - Current month
  - Previous months
  - Custom date range (optional for MVP if feasible)

### Summary Cards
Displayed as four cards in a responsive row/grid:

1. **Total Spent**
2. **Number of Expenses**
3. **Average Expense**
4. **Largest Expense**

#### Data Shown
| Card | Value |
|---|---|
| Total Spent | Sum of expense amounts in selected period |
| Number of Expenses | Count of expenses in selected period |
| Average Expense | Total / count |
| Largest Expense | Highest single expense amount in selected period |

### Acceptance Criteria
- Dashboard loads authenticated user data only
- Default period is current month
- Summary values update correctly when period changes
- Empty state is shown if user has no expenses
- Dashboard remains usable on desktop and tablet widths

---

## 8.3 Add Expense

### Purpose
Allow the user to record a new expense quickly and accurately.

### Entry Points
- Primary `Add Expense` button in dashboard header
- Floating action button on mobile/tablet layouts if implemented
- Dedicated `/expenses/new` page or modal

### UI Form
#### Fields
| Field | Type | Required | Notes |
|---|---|---:|---|
| Description | Text input | Yes | Main natural-language input |
| Amount | Decimal number input | Yes | Positive number only |
| Currency | Dropdown | Yes | Default set from user preference or system default |
| Date | Date picker | Yes | Defaults to today |
| Category | Dropdown | No | Optional if AI suggestion is used |
| Notes | Multiline text | No | Optional |
| Merchant | Text input | No | Optional in MVP if included |
| Use AI Suggestion | Button or automatic trigger | No | Suggests category from description |

### UX Behavior
- User enters description and amount first
- AI categorization can trigger:
  - automatically after description input loses focus, or
  - when clicking `Suggest Category`
- Suggested category appears in the category field
- User can manually override before saving

### Buttons
- Primary: `Save Expense`
- Secondary: `Cancel`
- Optional inline action: `Suggest Category`

### Validation Rules
- Description cannot be empty
- Amount must be greater than zero
- Date must be valid
- Category, if present, must be from allowed set
- Currency must be selected

### Success State
- Expense saved successfully
- Success toast or inline confirmation shown
- User redirected to expense list or dashboard, or modal closes and list refreshes

### Error States
- Missing required fields
- Invalid amount
- AI suggestion failed
- Server error while saving

### Acceptance Criteria
- User can save an expense with all required fields valid
- AI suggestion never blocks manual entry
- Manual category selection always overrides AI
- Newly created expense appears in list and analytics after save
- Expense is associated with the authenticated user

---

## 8.4 Expense List

### Purpose
Let users review, filter, edit, and delete previously entered expenses.

### Screen Layout
- Header: `Expenses`
- Filter bar
- Expense table or card list
- Pagination or infinite scroll
- Empty state if no expenses match filters

### Filters
| Filter | Type | Required |
|---|---|---:|
| Date range | Date inputs or picker | No |
| Category | Multi-select or dropdown | No |
| Minimum amount | Number input | No |
| Maximum amount | Number input | No |
| Search description | Text input | No |

### Expense Table Columns
| Column | Description |
|---|---|
| Date | Expense date |
| Description | User-entered text |
| Category | Assigned category |
| Amount | Decimal amount with currency |
| Source | Manual or AI-assisted, optional |
| Actions | Edit / Delete |

### Row Actions
#### Edit
- Opens edit form modal or page
- Pre-populates existing values

#### Delete
- Opens confirmation dialog
- Text: `Are you sure you want to delete this expense? This action cannot be undone.`

### Acceptance Criteria
- Only authenticated user expenses are shown
- Filters return correct subset of results
- Search works against description text
- Edit updates the selected record only
- Delete permanently removes the record
- Empty state is shown when no results match

---

## 8.5 Edit Expense

### Purpose
Allow users to correct or update previously logged expenses.

### UI
Same fields as Add Expense, pre-filled with current values.

### Editable Fields
- Description
- Amount
- Currency
- Date
- Category
- Notes
- Merchant, if included

### Success Conditions
- Updated expense saved
- Updated values reflected immediately in list and dashboard

### Acceptance Criteria
- Existing values load correctly
- Validation rules match Add Expense rules
- Category can be changed manually regardless of original source
- Analytics refresh to reflect updated values

---

## 8.6 Delete Expense

### Purpose
Allow users to remove incorrect entries.

### UX
- Delete action available from expense list and optionally from edit page
- Confirmation modal required

### Confirmation Modal
- Title: `Delete Expense`
- Body: `Are you sure you want to delete this expense?`
- Buttons:
  - `Delete`
  - `Cancel`

### Acceptance Criteria
- Expense is removed only after explicit confirmation
- Deleted expense no longer appears in list
- Dashboard and analytics update after deletion

---

## 8.7 Category System

### Purpose
Provide a consistent and understandable classification system for expenses.

### Default Categories
- Food
- Transport
- Bills
- Shopping
- Entertainment
- Health
- Education
- Rent
- Other

### MVP Product Decision
Default categories are system-defined and fixed for MVP.  
User-defined categories are optional and should be deferred unless capacity allows.

### UI Locations
- Category dropdown in Add/Edit Expense form
- Category filters in expense list
- Category labels in charts and expense entries

### Acceptance Criteria
- All expenses belong to a valid category
- AI output is mapped only to allowed categories
- Category names remain consistent across all screens

---

## 8.8 AI Category Suggestion

### Purpose
Reduce manual categorization effort by suggesting a category based on the expense description.

### User Experience
When entering a description such as `Uber ride to campus`, the system suggests `Transport`.

### Trigger Methods
Preferred MVP method:
- User enters description
- User clicks `Suggest Category`

Alternative:
- Auto-trigger after description entry if confidence and latency are acceptable

### UI Elements
- Inline button near category field: `Suggest Category`
- Loading indicator while request is in progress
- Suggested category displayed in category dropdown or badge
- Optional confidence label if included internally; not required to show to users in MVP

### User Feedback States
- Loading: `Getting suggestion...`
- Success: Category populated automatically
- Failure: Inline helper text: `Could not suggest a category. Please choose one manually.`

### Functional Behavior
- The backend sends the description text to Gemini
- Gemini must return one category from the allowed set
- Backend validates output before returning it to frontend
- Invalid or empty responses result in graceful fallback

### Acceptance Criteria
- Suggestion returns one valid category or a handled failure state
- User can always override the suggestion
- Suggestion is not saved until the expense itself is saved
- AI failures do not block expense creation
- Backend logs failures for debugging

---

## 8.9 Analytics Dashboard

### Purpose
Translate raw expense data into understandable spending patterns.

### Sections

#### A. Summary Cards
Already defined in Dashboard Home.

#### B. Category Breakdown
Display type:
- Bar chart preferred for clarity
- Pie chart optional if visual design supports readability

**Data shown**
- Category name
- Total amount spent in selected period
- Optional percentage of total

#### C. Monthly Trend
Display type:
- Line chart or column chart

**Data shown**
- Spending totals over time, grouped by month or week depending on selected range

#### D. Category Ranking
Optional list component:
- Top categories by spend in selected period

### Empty State
If no data exists:
- Show empty chart placeholders
- Message: `No spending data available for this period.`

### Acceptance Criteria
- Chart values match backend analytics
- Period changes update all sections consistently
- Empty state renders without broken layouts
- Data labels and currency formatting are consistent

---

## 8.10 AI or Rule-Based Insights

### Purpose
Give users concise, readable observations about their spending behavior.

### Example Insight Types
- `Your food spending increased by 18% compared to last month.`
- `Bills account for 41% of your total spending this month.`
- `This is your highest monthly spending in the last 3 months.`
- `Your largest single expense this month was Rent.`

### Product Decision
For MVP, deterministic analytics should be computed first. Insight text may be generated:
- fully by business rules, or
- by AI based only on structured analytics data

Preferred approach:
- Deterministic calculation
- Template-based phrasing for reliability
- AI phrasing optional if time allows

### UI
- Section title: `Insights`
- Individual insight cards or stacked callouts
- Each card contains:
  - Short title, optional
  - One sentence insight
  - Optional context line such as `Compared to last month`

### Acceptance Criteria
- Insights must be based only on actual analytics data
- No insight may reference nonexistent values
- Insights must remain understandable and concise
- If insufficient data exists, section should show: `Not enough data yet to generate insights.`

---

## 8.11 Empty States

### Purpose
Ensure the product feels complete even when no user data exists.

### Required Empty States
- New account with no expenses
- No expenses for selected period
- No filtered results in expenses list
- AI suggestion failed
- Analytics unavailable for empty dataset

### Example Messages
- `No expenses yet. Add your first expense to get started.`
- `No expenses match your filters.`
- `No insights available yet. Add more expenses to unlock insights.`

### Acceptance Criteria
- Every key screen with dynamic data has an empty state
- Empty states include a clear next action where relevant

---

## 8.12 Error Handling

### Purpose
Provide understandable and recoverable feedback when actions fail.

### Common Error Cases
- Authentication expired
- Network failure
- Failed save
- Failed delete
- Failed AI suggestion
- Server validation error

### UX Rules
- Use inline field errors for form validation
- Use toast or alert banners for action failures
- Never display raw backend stack traces
- Expired auth should redirect to login with message

### Acceptance Criteria
- Errors are visible and understandable
- User can recover without losing more data than necessary
- Form state remains preserved on recoverable failures where feasible

---

## 9. Detailed API Requirements

## 9.1 Authentication Endpoints
- `POST /auth/register`
- `POST /auth/login`
- `GET /auth/me`

### Data Collected
#### Register
- email
- password

#### Login
- email
- password

---

## 9.2 Expense Endpoints
- `POST /expenses`
- `GET /expenses`
- `GET /expenses/{id}`
- `PUT /expenses/{id}`
- `DELETE /expenses/{id}`

### Expense Data Model
| Field | Type | Required |
|---|---|---:|
| id | UUID / integer | System |
| user_id | UUID / integer | System |
| description | string | Yes |
| amount | decimal | Yes |
| currency | string | Yes |
| category | string | Yes |
| expense_date | date | Yes |
| notes | string | No |
| merchant | string | No |
| category_source | enum(manual, ai) | System |
| created_at | datetime | System |
| updated_at | datetime | System |

---

## 9.3 AI Endpoint
- `POST /ai/classify-expense`

### Data Collected
- description text

### Returned Data
- suggested category
- optional confidence
- optional status code / metadata internally

---

## 9.4 Analytics Endpoints
- `GET /analytics/monthly-summary`
- `GET /analytics/category-breakdown`
- `GET /analytics/trends`
- `GET /analytics/insights`

### Query Parameters
- month
- year
- start_date
- end_date
- category, where applicable

---

## 10. Data Requirements

## 10.1 Core Entities
- User
- Expense
- Category
- Insight cache, optional
- AI classification log, optional

## 10.2 Data Integrity Rules
- Every expense belongs to exactly one user
- Every expense has one valid category
- Amount must be positive
- expense_date cannot be null
- Deleting a user should cascade or archive expenses according to implementation decision; for MVP, user deletion is out of scope

---

## 11. Non-Functional Requirements

### 11.1 Performance
- Standard API endpoints: target < 500 ms median
- AI suggestion endpoint: target < 2 seconds median
- Dashboard first meaningful load: target < 2 seconds on broadband desktop

### 11.2 Security
- JWT authentication
- Password hashing with a strong algorithm
- HTTPS in production
- Input validation on all endpoints
- Server-side authorization checks on every user-owned resource
- Secrets stored in environment variables or a secure secret manager

### 11.3 Reliability
- Graceful degradation if AI provider is unavailable
- Core product remains usable without AI
- Logging for backend failures and AI errors

### 11.4 Scalability
- Stateless backend API
- Database indexing on user_id, expense_date, and category where helpful
- API design reusable for mobile clients

### 11.5 Accessibility
- Sufficient color contrast
- Keyboard-accessible forms
- Labels associated with inputs
- Error messages readable by assistive technologies where feasible

### 11.6 Localization
- English only for MVP unless broader support is intentionally added
- Currency display should be configurable even if language is not

---

## 12. Design Requirements

## 12.1 General UI Principles
- Clean and minimal visual hierarchy
- Primary action always clear
- Financial numbers should be easy to scan
- Avoid clutter in charts and tables

## 12.2 Responsive Design
Minimum supported layouts:
- Desktop
- Tablet
- Mobile-web basic responsiveness

### Responsive Priorities
- Dashboard cards stack appropriately
- Expense table may collapse into card list on smaller screens
- Add Expense form remains usable on narrow widths

## 12.3 Formatting Rules
- Currency formatting should be consistent across the product
- Dates should follow a consistent locale format
- Decimal precision should be standardized, typically two decimal places

---

## 13. Analytics Logic Requirements

## 13.1 Monthly Summary
Must compute:
- Total spent in selected period
- Number of expenses
- Average expense
- Largest expense

## 13.2 Category Breakdown
Must compute:
- Sum of expenses grouped by category
- Optional percentage share of total

## 13.3 Trend Analysis
Must compute:
- Time-grouped totals by month or week
- Comparison to previous period where applicable

## 13.4 Insight Eligibility Rules
Examples:
- Month-over-month increase insight only if both current and previous months have data
- Dominant category insight only if at least one category exceeds a meaningful threshold
- Largest expense insight only if at least one expense exists

---

## 14. User Stories

### Authentication
- As a new user, I want to create an account so I can securely store my expenses.
- As a returning user, I want to log in so I can access my spending history.

### Expense Logging
- As a user, I want to add an expense quickly so I can keep records without friction.
- As a user, I want the system to suggest a category from text so I do not need to categorize everything manually.
- As a user, I want to override the suggested category so I stay in control.

### Expense Management
- As a user, I want to view all my expenses so I can review my spending.
- As a user, I want to filter expenses by category and date so I can find specific entries.
- As a user, I want to edit or delete an expense so I can correct mistakes.

### Analytics
- As a user, I want to see my monthly total so I understand overall spending.
- As a user, I want to see category breakdowns so I know where most of my money goes.
- As a user, I want concise insights so I can notice patterns quickly.

### Reliability
- As a user, I want to keep using the app even if AI suggestion fails so the product remains useful.

---

## 15. User Acceptance Criteria by Feature

## 15.1 Authentication
- Given I am a new user, when I submit valid registration details, then my account is created and I am authenticated.
- Given I provide an already-used email, when I try to register, then I see a clear error.
- Given I am unauthenticated, when I request protected resources, then access is denied.

## 15.2 Add Expense
- Given I enter description, amount, date, and currency, when I save, then the expense is created.
- Given AI suggestion fails, when I manually choose a category and save, then the expense is still created.
- Given I enter an invalid amount, when I submit, then the form shows a validation error.

## 15.3 Expense List
- Given I have expenses, when I open the list, then I can see only my own expenses.
- Given I apply filters, when results exist, then only matching expenses are shown.
- Given I apply filters and no results exist, then I see an empty-state message.

## 15.4 Edit and Delete
- Given an existing expense, when I edit and save valid values, then the expense updates successfully.
- Given an existing expense, when I confirm deletion, then the expense is removed and no longer appears.

## 15.5 AI Suggestion
- Given a valid description, when I request suggestion, then I receive one allowed category or a handled failure state.
- Given a suggested category, when I change it manually, then the manually selected category is used.

## 15.6 Analytics
- Given expenses exist in the selected period, when I open the dashboard, then summary numbers and charts reflect those expenses accurately.
- Given no data exists in the selected period, when I open analytics, then I see empty states instead of broken charts.

## 15.7 Insights
- Given sufficient data exists, when insights are generated, then they match actual analytics values.
- Given insufficient data, when I open insights, then I see a clear insufficient-data message.

---

## 16. Dependencies

### External Dependencies
- Google Gemini API via Google AI Studio
- PostgreSQL database
- Hosting environment for backend and frontend

### Internal Dependencies
- Authentication must exist before protected expense endpoints
- Expense data must exist before analytics are meaningful
- Deterministic analytics must exist before AI insight phrasing is useful

---

## 17. Constraints

- AI quota and latency may limit aggressive auto-triggering
- MVP team capacity may require deferring advanced features
- Product must remain valuable even without AI
- Budget may constrain hosting choices initially

---

## 18. Risks and Mitigations

| Risk | Description | Mitigation |
|---|---|---|
| AI misclassification | Suggested category may be wrong | Allow manual override at all times |
| AI downtime or latency | Suggestion may fail or feel slow | Make AI optional and non-blocking |
| Poor user retention | Users may stop logging expenses | Minimize friction and show quick value on dashboard |
| Incorrect analytics | Trust is lost if numbers are wrong | Compute analytics deterministically and test thoroughly |
| Scope creep | Product may expand too early | Keep MVP limited to core flows |

---

## 19. Milestones and Timeline

### Phase 0: Planning and Setup (Week 1)
- Finalize PRD
- Define architecture
- Set up repositories
- Define database schema
- Establish design system direction

### Phase 1: Backend Foundation (Week 2)
- Authentication API
- User entity
- JWT integration
- Database connectivity

### Phase 2: Expense Core (Week 3)
- Expense entity
- Expense CRUD endpoints
- Validation rules
- Category system

### Phase 3: Frontend Core (Week 4)
- Auth screens
- Dashboard shell
- Add Expense flow
- Expense list

### Phase 4: Analytics (Week 5)
- Monthly summary endpoint
- Category breakdown endpoint
- Trend endpoint
- Dashboard chart integration

### Phase 5: AI Suggestion (Week 6)
- Gemini integration
- Backend validation for category suggestions
- Frontend suggestion UX
- Failure handling

### Phase 6: Insights and Polish (Week 7)
- Rule-based insights
- Empty states
- Error handling
- QA and bug fixing

### Phase 7: Deployment (Week 8)
- Production environment setup
- Environment variable management
- Monitoring/logging basics
- Launch-ready validation

---

## 20. Definition of Done

A feature is considered done when:
- Functional requirements are implemented
- Acceptance criteria pass
- UI states are complete, including loading, success, empty, and error where applicable
- Authorization and validation are enforced
- Relevant tests are added
- API behavior is documented
- No critical bugs remain open for that feature

---

## 21. Open Questions

- Should custom categories be included in MVP or deferred?
- Should AI suggestion trigger automatically or only on click?
- Should confidence score be surfaced in UI or kept internal?
- What is the exact default currency for first-time users?
- Will dashboard support custom date range in MVP or only month selection?

---

## 22. Launch Readiness Checklist

- Authentication works end-to-end
- Expense create, edit, delete all function correctly
- Dashboard values match seeded and manual test data
- AI suggestion behaves gracefully on success and failure
- No unauthorized data access is possible
- Empty states and error states are implemented
- Deployment environment is stable
- Basic monitoring/logging is enabled

---

## 23. Appendix: Recommended Initial Screen List

1. Landing page
2. Sign Up
3. Login
4. Dashboard
5. Expenses list
6. Add Expense modal or page
7. Edit Expense modal or page
8. Account/Profile page, minimal MVP version

---

## 24. Final Product Statement

Smart Expense Tracker with AI Insights is a personal finance web application designed to make expense tracking fast, clear, and useful. It combines secure account-based expense management, deterministic analytics, and AI-assisted categorization to reduce effort while improving financial awareness. The MVP is intentionally focused: fast input, reliable summaries, and actionable insights, all delivered through an API-first architecture that supports future mobile expansion.
