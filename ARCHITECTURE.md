# Angular 22 Multi-Layout Application Architecture

## Overview

This is a modern Angular 22 application featuring three distinct layouts with shared components, reactive forms, and Bootstrap 5 styling.

## Project Structure

```
src/app/
├── layouts/
│   ├── admin-layout/          # Admin section layout with sidebar
│   ├── dashboard-layout/       # User dashboard layout
│   └── public-layout/          # Public/home layout
├── pages/
│   ├── home/                  # Home page
│   ├── admin-dashboard/       # Admin dashboard page
│   ├── user-dashboard/        # User dashboard page
│   └── sample-form/           # Example reactive form
├── shared/
│   ├── header/                # Shared header component
│   └── footer/                # Shared footer component
├── services/
│   └── title.ts               # Dynamic title service for API-driven titles
├── app.routes.ts              # Application routing configuration
├── app.config.ts              # Application configuration with HTTP client
└── app.ts                      # Root application component
```

## Key Features

### 1. Three Layout System

- **Public Layout**: Used for home page and general content
- **Dashboard Layout**: Full-width layout for user dashboard
- **Admin Layout**: Sidebar navigation layout for admin pages

Each layout includes:

- Header component with dynamic title
- Footer component with links
- Content area with router outlet
- Bootstrap 5 styling

### 2. Dynamic Page Titles

- Titles are managed by `TitleService` using RxJS `BehaviorSubject`
- Components set titles on initialization: `this.titleService.setTitle('Page Name')`
- Header component displays title from API call via Observable
- Can fetch titles from API using `fetchTitleFromApi(endpoint)`

### 3. Reactive Forms

- Sample form demonstrates best practices with `FormBuilder` and `Validators`
- Form validation includes:
    - Required fields
    - Email validation
    - Pattern matching (phone)
    - Minimum length constraints
- Validation error messages displayed dynamically
- Bootstrap form styling with error states

### 4. Bootstrap 5 Integration

- Navbar with responsive toggling
- Card components for content sections
- Alert boxes for notifications
- Form controls with validation styling
- Table layouts for displaying data
- Grid system for responsive layouts

## Routing Configuration

```
/                          → PublicLayout + Home
/form                      → PublicLayout + SampleForm
/user-dashboard            → DashboardLayout + UserDashboard
/admin                     → AdminLayout + AdminDashboard
/admin/dashboard           → AdminLayout + AdminDashboard
```

## Services

### TitleService

Provides Observable-based title management:

```typescript
// Set title directly
titleService.setTitle('My Page Title');

// Get title as Observable
title$ = titleService.title$;

// Fetch title from API
titleService.fetchTitleFromApi('/api/page-title')
  .subscribe(title => titleService.setTitle(title));
```

## Components

### Header (Shared)

- Navigation bar with links to all main sections
- Displays current page title from `TitleService`
- Responsive Bootstrap navbar

### Footer (Shared)

- Company information
- Quick links
- Contact information

### Layouts

Each layout component:

- Imports router outlet for page content
- Includes header and footer
- Provides specific layout styling (sidebar for admin, etc.)

### Pages

- **Home**: Welcome page with feature highlights
- **AdminDashboard**: System statistics display with sidebar menu
- **UserDashboard**: User profile and activity log
- **SampleForm**: Complete example of reactive form with validation

## Styling

- **Global styles**: `src/styles.scss`
- **Component styles**: Each component has its own `.scss` file
- **Bootstrap**: Configured in `angular.json` for global inclusion
- **Responsive**: Mobile-first design using Bootstrap grid system

## Running the Application

### Development Server

```bash
npm start
# Navigate to http://localhost:4200/
```

### Build for Production

```bash
npm run build
# Output: dist/angular-app/
```

### Running Tests

```bash
npm run test
```

## Angular Features Used

- Standalone components (no NgModules)
- Component-based routing
- Reactive Forms with FormBuilder
- RxJS Observables
- Dependency Injection
- Angular HttpClient
- Bootstrap CSS Framework

## Browser Support

Angular 22 supports:

- Chrome (latest)
- Firefox (latest)
- Safari (latest)
- Edge (latest)

## Future Enhancements

1. **Authentication**: Add auth guards to protected routes
2. **State Management**: Implement NgRx for complex state
3. **API Integration**: Connect TitleService to real APIs
4. **Form Validation**: Extend with custom validators
5. **Theme Support**: Add dark mode with CSS variables
6. **Internationalization**: Add i18n for multi-language support
7. **Error Handling**: Global error handler and logging
