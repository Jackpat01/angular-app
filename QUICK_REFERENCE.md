# Quick Reference - Angular 22 Multi-Layout App

## 🚀 Start Development
```bash
cd /root/angular-app
npm start
```
Open http://localhost:4200

## 📄 File Locations

### Key Files
- Routes: `src/app/app.routes.ts`
- Configuration: `src/app/app.config.ts`
- Title Service: `src/app/services/title.ts`
- Global Styles: `src/styles.scss`

### Layouts
- Public: `src/app/layouts/public-layout/`
- Dashboard: `src/app/layouts/dashboard-layout/`
- Admin: `src/app/layouts/admin-layout/`

### Shared Components
- Header: `src/app/shared/header/`
- Footer: `src/app/shared/footer/`

### Pages
- Home: `src/app/pages/home/`
- User Dashboard: `src/app/pages/user-dashboard/`
- Admin Dashboard: `src/app/pages/admin-dashboard/`
- Contact Form: `src/app/pages/sample-form/`

## 📍 Application Routes

| URL | Layout | Component |
|-----|--------|-----------|
| `/` | Public | Home |
| `/form` | Public | Sample Form |
| `/user-dashboard` | Dashboard | User Dashboard |
| `/admin` | Admin | Admin Dashboard |
| `/admin/dashboard` | Admin | Admin Dashboard |

## 💻 Common Tasks

### Create New Page
```bash
npx ng generate component pages/my-page --skip-tests
```

### Create New Service
```bash
npx ng generate service services/my-service --skip-tests
```

### Set Page Title
```typescript
constructor(private titleService: TitleService) {}

ngOnInit(): void {
  this.titleService.setTitle('My Page Title');
}
```

### Fetch Title from API
```typescript
ngOnInit(): void {
  this.titleService.fetchTitleFromApi('/api/titles/123')
    .subscribe();
}
```

### Create Reactive Form
```typescript
import { FormBuilder, Validators } from '@angular/forms';

constructor(private fb: FormBuilder) {}

ngOnInit(): void {
  this.form = this.fb.group({
    name: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]]
  });
}
```

### Add New Route
Edit `src/app/app.routes.ts`:
```typescript
{
  path: 'my-path',
  component: PublicLayout,  // or other layout
  children: [
    { path: '', component: MyPage }
  ]
}
```

## 🎨 Bootstrap Classes

### Grid
```html
<div class="row">
  <div class="col-md-6">Half width</div>
  <div class="col-md-6">Half width</div>
</div>
```

### Cards
```html
<div class="card">
  <div class="card-header">Header</div>
  <div class="card-body">Content</div>
</div>
```

### Buttons
```html
<button class="btn btn-primary">Primary</button>
<button class="btn btn-secondary">Secondary</button>
<button class="btn btn-danger">Danger</button>
```

### Alerts
```html
<div class="alert alert-success">Success</div>
<div class="alert alert-warning">Warning</div>
<div class="alert alert-danger">Error</div>
```

### Forms
```html
<input class="form-control" placeholder="Name">
<select class="form-select">
  <option>Option 1</option>
</select>
<textarea class="form-control"></textarea>
```

## 🔍 Validation Rules

### Required
```typescript
Validators.required
```

### Email
```typescript
Validators.email
```

### Min/Max Length
```typescript
Validators.minLength(2)
Validators.maxLength(50)
```

### Pattern
```typescript
Validators.pattern(/^[0-9\-\+\(\)]{10,}$/)  // Phone
```

### Multiple Validators
```typescript
firstName: ['', [Validators.required, Validators.minLength(2)]]
```

## 📦 Dependencies

- Angular 22
- Bootstrap 5.3.3
- RxJS
- TypeScript

Install new package:
```bash
npm install package-name
```

## 🏗️ Component Template

```typescript
import { Component, OnInit } from '@angular/core';
import { TitleService } from '../../services/title';

@Component({
  imports: [],
  selector: 'app-my-component',
  styleUrl: './my-component.scss',
  templateUrl: './my-component.html',
})
export class MyComponent implements OnInit {
  constructor(private titleService: TitleService) {}

  ngOnInit(): void {
    this.titleService.setTitle('My Component');
  }
}
```

## 🔧 Build Commands

| Command | Purpose |
|---------|---------|
| `npm start` | Start dev server |
| `npm run build` | Production build |
| `npm run test` | Run tests |
| `npm run lint` | Lint code |

## 📂 Project Structure

```
src/
├── app/
│   ├── layouts/          ← Layout components
│   ├── pages/            ← Page components
│   ├── shared/           ← Shared components
│   ├── services/         ← Services
│   ├── app.routes.ts     ← Routes
│   ├── app.config.ts     ← Configuration
│   └── app.ts            ← Root component
├── styles.scss           ← Global styles
└── main.ts               ← Entry point
```

## 📖 Documentation

- **ARCHITECTURE.md** - Detailed system design
- **GETTING_STARTED.md** - Comprehensive guide
- **PROJECT_SUMMARY.md** - Overview and checklist
- **QUICK_REFERENCE.md** - This file

## 🐛 Debugging

### Check Console
Open DevTools (F12) and look at Console tab for errors

### Check Network
Network tab shows HTTP requests/responses

### Check Component
Angular DevTools extension for debugging components

## 🌐 Browser DevTools

1. Right-click → Inspect (or F12)
2. Check Console for errors
3. Check Network for API calls
4. Check Elements for HTML structure

## 💡 Tips

- Always set page title in `ngOnInit()`
- Use FormBuilder for complex forms
- Import CommonModule for *ngIf, *ngFor
- Use `async` pipe for Observables in templates
- Keep components small and focused
- Use services for shared logic

## 📚 Resources

- [Angular Docs](https://angular.io)
- [Bootstrap Docs](https://getbootstrap.com)
- [TypeScript Docs](https://www.typescriptlang.org)
- [RxJS Docs](https://rxjs.dev)

## 🆘 Common Issues

**Build fails**
- Check TypeScript errors
- Verify imports
- Clear node_modules: `rm -rf node_modules && npm install`

**Forms not working**
- Import ReactiveFormsModule
- Check formControlName matches property names
- Verify form group structure

**Styles not applied**
- Check Bootstrap CSS is loaded
- Verify class names are correct
- Check CSS specificity

## ✨ Next: Add Feature

1. Generate new component
2. Add to appropriate layout
3. Set title
4. Add route
5. Update navigation
6. Test in browser
