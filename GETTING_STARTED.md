# Getting Started with Angular 22 Multi-Layout App

## Installation

### Prerequisites

- Node.js 18+ and npm 10+
- Angular CLI 22+

### Setup

```bash
# Navigate to the project directory
cd angular-app

# Install dependencies (already done)
npm install

# Start development server
npm start

# Open http://localhost:4200 in your browser
```

## Project Layout

The application includes three main sections accessible from the navigation:

### 1. Home Page (`/`)

- Welcome section with feature overview
- Links to explore Dashboard and Forms
- Located in Public Layout

### 2. User Dashboard (`/user-dashboard`)

- User profile information
- Recent activity log
- Located in Dashboard Layout

### 3. Admin Section (`/admin`)

- System statistics
- Sidebar navigation menu
- Located in Admin Layout

## Adding New Pages

### Step 1: Create Component

```bash
npx ng generate component pages/my-page --skip-tests
```

### Step 2: Update Component with Title Service

Edit `src/app/pages/my-page/my-page.ts`:

```typescript
import { Component, OnInit } from '@angular/core';
import { TitleService } from '../../services/title';

@Component({
  imports: [],
  selector: 'app-my-page',
  styleUrl: './my-page.scss',
  templateUrl: './my-page.html',
})
export class MyPage implements OnInit {
  constructor(private titleService: TitleService) {}

  ngOnInit(): void {
    this.titleService.setTitle('My Page Title');
  }
}
```

### Step 3: Add Route

Update `src/app/app.routes.ts`:

```typescript
{
  path: 'my-path',
  component: PublicLayout,  // or DashboardLayout, AdminLayout
  children: [
    { path: '', component: MyPage }
  ]
}
```

## Creating Reactive Forms

### Example: Simple Contact Form

```typescript
import { FormBuilder, FormGroup, Validators } from '@angular/forms';

export class MyForm implements OnInit {
  myForm!: FormGroup;

  constructor(private fb: FormBuilder) {}

  ngOnInit(): void {
    this.myForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      message: ['', [Validators.required]]
    });
  }

  onSubmit(): void {
    if (this.myForm.valid) {
      console.log(this.myForm.value);
    }
  }
}
```

### Form Template with Bootstrap

```html
<form [formGroup]="myForm" (ngSubmit)="onSubmit()">
  <div class="mb-3">
    <label for="name" class="form-label">Name</label>
    <input 
      type="text" 
      class="form-control" 
      id="name"
      formControlName="name"
      [class.is-invalid]="myForm.get('name')?.invalid && myForm.get('name')?.touched">
  </div>
  
  <button type="submit" class="btn btn-primary" [disabled]="myForm.invalid">
    Submit
  </button>
</form>
```

## Using Dynamic Titles from API

### Update TitleService to Fetch from API

```typescript
fetchTitleFromApi(endpoint: string): Observable<string> {
  return this.http.get<{ title: string }>(endpoint).pipe(
    map(response => response.title),
    tap(title => this.setTitle(title))
  );
}
```

### Use in Component

```typescript
ngOnInit(): void {
  this.titleService.fetchTitleFromApi('/api/page-titles/dashboard')
    .subscribe();
}
```

## Bootstrap Components Used

### Navbar

```html
<nav class="navbar navbar-expand-lg navbar-dark bg-dark">
  <!-- navbar content -->
</nav>
```

### Cards

```html
<div class="card">
  <div class="card-header">Title</div>
  <div class="card-body">Content</div>
</div>
```

### Alerts

```html
<div class="alert alert-success">Success message</div>
<div class="alert alert-warning">Warning message</div>
<div class="alert alert-danger">Error message</div>
```

### Forms

```html
<input class="form-control" placeholder="Enter text">
<select class="form-select">
  <option>Option 1</option>
</select>
<textarea class="form-control"></textarea>
```

### Buttons

```html
<button class="btn btn-primary">Primary</button>
<button class="btn btn-secondary">Secondary</button>
<button class="btn btn-danger">Danger</button>
```

### Grid System

```html
<div class="row">
  <div class="col-md-4">25% width on medium+ screens</div>
  <div class="col-md-8">75% width on medium+ screens</div>
</div>
```

## Building for Production

```bash
# Create optimized production build
npm run build

# Output located in dist/angular-app/
# Serve the dist folder with any HTTP server
```

## Adding New Layouts

### Step 1: Generate Component

```bash
npx ng generate component layouts/my-layout --skip-tests
```

### Step 2: Update Layout Component

```typescript
import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Header } from '../../shared/header/header';
import { Footer } from '../../shared/footer/footer';

@Component({
  imports: [RouterOutlet, Header, Footer],
  selector: 'app-my-layout',
  styleUrl: './my-layout.scss',
  templateUrl: './my-layout.html',
})
export class MyLayout {}
```

### Step 3: Design Layout Template

```html
<app-header></app-header>
<main class="main-content">
  <!-- Your custom layout structure -->
  <router-outlet></router-outlet>
</main>
<app-footer></app-footer>
```

### Step 4: Use in Routes

```typescript
{
  path: 'my-section',
  component: MyLayout,
  children: [
    { path: '', component: MyPage }
  ]
}
```

## Tips & Best Practices

1. **Always set page titles** in component's `ngOnInit()`
2. **Use FormBuilder** for complex forms instead of manual FormGroup creation
3. **Add proper validation** to all form fields
4. **Use Bootstrap utilities** for spacing, alignment, and responsive design
5. **Leverage Observables** for async operations with `async` pipe
6. **Keep components focused** - one responsibility per component
7. **Use services** for shared logic and data management
8. **Style with SCSS** - leverage nesting and variables

## Common Commands

```bash
# Generate new component
npx ng generate component path/my-component --skip-tests

# Generate service
npx ng generate service services/my-service --skip-tests

# Start dev server
npm start

# Build for production
npm run build

# Run tests
npm run test

# Format code
npm run lint
```

## Troubleshooting

### Build Bundle Too Large

- Check `angular.json` for unused styles
- Use lazy loading for routes
- Analyze bundle: `npm run build -- --stats-json`

### Forms Not Validating

- Import `ReactiveFormsModule` in component
- Use proper form control names
- Check for typos in `formControlName` attributes

### Routing Issues

- Ensure routes are configured in `app.routes.ts`
- Check component imports in layout/page components
- Verify correct use of `RouterOutlet`

## Next Steps

1. Connect to a real backend API
2. Add authentication and route guards
3. Implement state management (NgRx)
4. Add error handling and logging
5. Create additional pages and forms
6. Customize styling for your brand
7. Add internationalization (i18n)
8. Implement unit tests

For more information, visit the [Angular Documentation](https://angular.io/docs).
