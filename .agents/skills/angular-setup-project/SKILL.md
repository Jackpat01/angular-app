---
name: angular-setup-project
description: Generic workflow for bootstrapping a production-ready Angular project with modern defaults (latest Angular CLI, detected package manager, default test runner), modular feature selection, and Angular 22 architecture patterns.
---

# Angular Setup Project Skill

This skill provides a standardized workflow for bootstrapping a production-ready Angular application with modern defaults and modular feature selection. All generated code follows the Angular 22 architecture patterns defined in this skill.

## Prerequisites Check

Before beginning setup, verify the availability of critical dependencies:

**System Requirements:**

- **Node.js** → Active LTS version (v18+ recommended)
- **Angular CLI** → Latest version (`npm install -g @angular/cli`)
- **Package Manager** → pnpm, npm, or yarn (detect from existing lockfile or ask user)

**Critical (Quality Foundation):**

- `angular-linters` skill → Required for ESLint, Stylelint, Husky, and Commitlint setup

**Action:** Validate system requirements:

1. Check Node.js: `node --version` (should be v18+)
2. Check Angular CLI: `ng version` (install if missing)
3. Check package manager: detect from existing lockfile (`pnpm-lock.yaml` / `yarn.lock` / `package-lock.json`) or ask the user

**Action: Safety Check (EXISTING PROJECT)**:

1.  **Verify**: Before running `ng new`, check if `angular.json` already exists in the current directory or parent directories.
2.  **Stop**: If an existing Angular project is detected, **ABORT**. Inform the user: _"An existing Angular project was detected in this directory. This skill only supports bootstrapping new projects."_

**Action: If critical skills are missing:**

1. Inform the user: _"The `angular-linters` skill is missing. This skill is required for setting up code quality tools."_
2. Offer alternatives:
   - Provide the skill file if available
   - Continue with manual linter setup (less reliable)

**Failure Recovery:**

> **Critical:** If the creation or scaffolding phase fails, the Agent **MUST** delete the generated project directory before attempting validation or retrying. Do not attempt to "repair" a failed `ng new` execution.

---

## System Contract

This skill is the orchestrator. It invokes sub-skills and passes context via a **project manifest** file.

### Project Manifest (`PROJECT_MANIFEST.json`)

At the end of Phase 1, generate this file in the project root:

```json
{
  "projectName": "<name>",
  "angularMajorVersion": 0,
  "packageManager": "pnpm | npm | yarn",
  "style": "css | scss",
  "ssr": false,
  "zoneless": true,
  "routing": true,
  "rxjsLinting": "auto | enabled | disabled",
  "budgetProfile": "strict | balanced | permissive",
  "skillVersions": {
    "angular-setup-project": "1.1",
    "angular-linters": "1.0",
    "angular-third-party-integration": "1.0"
  }
}
```

### Sub-Skill Contracts

| Sub-Skill | Reads From Manifest | Writes to Manifest | Success Condition | Failure Behavior |
|---|---|---|---|---|
| `angular-linters` | `packageManager`, `style`, `angularMajorVersion`, `rxjsLinting` | Adds `"lintersConfigured": true` | All Step 6 checks pass (lint, lint:styles, format exit 0) | Scoped rollback of changed files; set `"lintersConfigured": false` |
| `angular-third-party-integration` | All fields + `lintersConfigured` | Adds `"integrations": [...]` | All libraries verified and committed | Scoped rollback per library |

### Invocation Protocol

1. The calling skill **MUST** write `PROJECT_MANIFEST.json` before invoking a sub-skill.
2. The sub-skill **MUST** read `PROJECT_MANIFEST.json` as its first step.
3. If `PROJECT_MANIFEST.json` is missing, the sub-skill **MUST** stop and ask the user for the required values (`packageManager`, `style`, `angularMajorVersion`) before proceeding. Do NOT assume defaults.

---

## Angular 22 Architecture Patterns (Mandatory Coding Standards)

> **These patterns are MANDATORY** for all code generated in this project. Apply them consistently throughout the codebase. When reviewing or writing any Angular code, verify compliance with every applicable pattern.

### Pattern 1: Feature-First Architecture (Structural)

The scaffold creates three top-level folders inside `src/app/`. Use them strictly:

| Folder | Purpose | Sub-structure |
|---|---|---|
| `core/` | Cross-cutting concerns shared across **all** features | `auth/`, `guards/`, `interceptors/`, `models/`, `services/` |
| `shared/` | Reusable UI and base classes with **zero** business logic | `base/` (abstract component classes), `components/` (pure UI) |
| `features/` | Self-contained feature modules — one subfolder per feature | Each feature gets: `models/`, `services/`, `pages/`, optionally `guards/` and `facades/` |

**Decision rule:** When adding any new file, ask: *"Does this belong to exactly one feature, or is it used by multiple?"*
- One feature → `features/[feature-name]/`
- Multiple features → `core/` (services/models) or `shared/` (UI components)

**Removing a feature should be as simple as deleting its folder.**

### Pattern 2: Standalone Components (Default — No NgModule)

All components are standalone. **Never use NgModule** unless a third-party library explicitly requires it.

```typescript
@Component({
  selector: 'app-example',
  imports: [CommonModule, RouterLink],  // ← explicit, visible dependencies
  templateUrl: './example.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExampleComponent { }
```

### Pattern 3: Container / Presentational Components

**Always separate data-fetching logic from display logic.**

- **Container (page-level)**: injects services/facades, manages state, orchestrates API calls, passes data down.
- **Presentational**: receives data via `input.required<T>()`, emits user actions via `output<T>()`. Has **zero** service injections.

Use the modern signal-based input/output API — not `@Input`/`@Output` decorators:

```typescript
// ✅ Presentational — receives data, emits events
@Component({ selector: 'app-user-list', ... })
export class UserListComponent {
  users = input.required<User[]>();
  deleteUser = output<number>();
}

// ✅ Container — owns the data
@Component({ selector: 'app-users-page', ... })
export class UsersPageComponent {
  private facade = inject(UserFacade);
  users = this.facade.users;
  removeUser(id: number) { this.facade.removeUser(id); }
}
```

**Rule:** A presentational component must have no `inject()` calls for services — only `input()` / `output()`.

### Pattern 4: Signal-Based State

**Use `signal()` for mutable state. Use `computed()` for derived state. Never duplicate state.**

```typescript
// ✅ Store minimum state; derive everything else
items = signal<Product[]>([]);
totalItems = computed(() => this.items().length);
totalPrice = computed(() => this.items().reduce((sum, p) => sum + p.price, 0));

// ❌ Never maintain redundant signals that must be kept in sync manually
totalItems = signal(0);   // bad — duplicates items.length
totalPrice = signal(0);   // bad — duplicates a derivable value
```

**Additional rules:**
- Always use `ChangeDetectionStrategy.OnPush` on components that read signals.
- Expose writable signals as readonly outside the owning class: `readonly items = this._items.asReadonly()`.
- Prefer `signal()` for local state; reserve `BehaviorSubject`/`Observable` for event streams, WebSockets, and multi-source compositions.
- Replace `takeUntil(destroy$)` with `takeUntilDestroyed(destroyRef)` — no manual `OnDestroy` needed.

### Pattern 5: Facade Pattern

**For every feature, create a `[feature].facade.ts`** as the single boundary between the UI layer and the data/state layer.

```
Component → Facade → Services / State → Backend
```

A Facade:
- Owns feature-level `signal()` state (private writable, public readonly)
- Exposes `computed()` derived state
- Wraps API calls and business logic methods
- Is `providedIn: 'root'` (or at the feature route level for lazy-loaded features)

```typescript
@Injectable({ providedIn: 'root' })
export class UserFacade {
  private readonly userApi = inject(UserApiService);
  private readonly _users = signal<User[]>([]);

  readonly users = this._users.asReadonly();
  readonly activeUsers = computed(() => this._users().filter(u => u.active));

  loadUsers(): void {
    this.userApi.getAll().subscribe(users => this._users.set(users));
  }

  removeUser(id: number): void {
    this._users.update(users => users.filter(u => u.id !== id));
  }
}
```

**Rule:** A page component should inject exactly **one** facade — not multiple services. If a page needs data from two domains, create a composite facade or reconsider the page design.

### Pattern 6: Reactive Server-State with `httpResource`

For read-only server data, prefer `httpResource` over manual Observable + loading/error state management:

```typescript
import { httpResource } from '@angular/common/http';

export class UsersComponent {
  users = httpResource<User[]>(() => '/api/users');
}
```

```html
@if (users.isLoading()) {
  <p>Loading...</p>
} @else if (users.error()) {
  <p>Unable to load users.</p>
} @else {
  @for (user of users.value() ?? []; track user.id) {
    <p>{{ user.name }}</p>
  }
}
```

**Use `httpResource` for:** read-only GET requests where `value`, `isLoading`, and `error` are displayed in the template.
**Use Observables for:** mutations (POST/PUT/DELETE), WebSockets, and complex multi-source stream compositions.

### Pattern 7: Dependency Injection + Strategy Pattern

For behaviour that varies by context (discount rules, validators, formatters), use an `InjectionToken` with a strategy interface instead of `if/else` chains:

```typescript
export interface DiscountStrategy {
  calculate(total: number): number;
}

export const DISCOUNT_STRATEGY =
  new InjectionToken<DiscountStrategy>('DISCOUNT_STRATEGY');

// Provide at route or component level:
providers: [{ provide: DISCOUNT_STRATEGY, useClass: PremiumDiscountStrategy }]

// Consume — the component does not know the implementation:
export class CheckoutComponent {
  private discount = inject(DISCOUNT_STRATEGY);
  finalPrice(total: number) { return this.discount.calculate(total); }
}
```

This keeps components open for extension and closed for modification (OCP).

### Pattern 8: Lazy Loading + Deferred UI

**All feature routes MUST use `loadComponent()`** — never static component imports in `app.routes.ts`:

```typescript
// ✅ Lazy-loaded routes
export const routes: Routes = [
  {
    path: 'users',
    loadComponent: () =>
      import('./features/users/pages/users-page/users-page')
        .then(m => m.UsersPageComponent),
  },
];
```

**Use route-level guards as functional `CanActivateFn`** — not class-based `CanActivate`:

```typescript
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.isAuthenticated()
    ? true
    : router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};
```

**Use `@defer` for heavy below-the-fold components:**

```html
@defer (on viewport) {
  <app-analytics-chart />
} @placeholder {
  <p>Chart will load here...</p>
}
```

Apply `@defer` to: charts, analytics widgets, editors, admin-only sections, and heavy third-party components.

### Bonus: Signal Forms

For new signal-first forms, prefer the Signal Forms API over Reactive Forms:

```typescript
import { email, form, minLength, required } from '@angular/forms/signals';

loginModel = signal({ email: '', password: '' });
loginForm = form(this.loginModel, path => {
  required(path.email);
  email(path.email);
  required(path.password);
  minLength(path.password, 8);
});
```

**Migration rule:** Do not rewrite existing Reactive Forms. Use Signal Forms only for new forms in new features.

---

## Phase 0: Version Detection (MANDATORY)

Before any interactive discovery, detect the Angular CLI version and derive defaults dynamically:

1. Run `ng version` and parse the **major version** number.
2. Run `ng new --help` and inspect the available flags and their defaults:
   - **Zoneless**: If a `--zoneless` flag exists, check its default value. If the flag defaults to `true`, zoneless is the CLI default. If the flag does not exist or defaults to `false`, zone.js is the default.
   - **Other flags**: Parse current defaults for `--ssr`, `--routing`, `--style`, etc. rather than assuming them.
3. Record the detected major version and flag defaults for use in the Interactive Discovery phase.

> **NEVER** hardcode Angular version-specific behavior. Always derive from the installed CLI. This ensures the skill works across Angular versions without manual updates.

---

## Workflow: Interactive Discovery

> **CRITICAL:** You **MUST NOT** assume preferences from previous conversations or context. **ALWAYS** present the detected defaults (from Phase 0) and ask for explicit confirmation before running `ng new`. Even if the user seems to want the same setup as before, ask.

1.  **Discovery**: Ask: "What foundational Angular features would you like to configure (e.g., SSR, Routing, Zoneless, CSS/SCSS)?"
2.  **Present Defaults** (derived from Phase 0): Show the detected CLI defaults and ask for confirmation or changes:
    - **Package Manager**: Detected from lockfile, or ask (pnpm, npm, yarn)
    - **Styles**: `css` (default) or `scss`
    - **SSR**: CLI default (detected in Phase 0)
    - **Routing**: CLI default (detected in Phase 0)
    - **Zoneless**: CLI default (detected in Phase 0) — if zoneless is the default, ask: "Would you like to disable zoneless and enable zone.js (legacy mode)?"
    - **RxJS Linting**: `auto` (default — detect from `package.json` after creation), `enabled`, or `disabled`
    - **Budget Profile**: `balanced` (default), `strict`, or `permissive`
      - **Strict** (small apps, no heavy third-party libs): warning `500kB` / error `1MB`
      - **Balanced** (typical apps with a component library): warning `2MB` / error `4MB`
      - **Permissive** (apps with heavy third-party like maps, charts, rich editors): warning `4MB` / error `8MB`
    - **Environment files**: `optional`
3.  **Core Focus**: Do NOT suggest advanced features like Tailwind CSS, Spartan UI, or SignalStore in this phase.
4.  **Quality Layer**: The quality stack (ESLint, Stylelint, Prettier, Husky, Commitlint) is **MANDATORY** and always included. Do NOT list it as an optional integration or ask the user whether they want it.

---

## Technical Phases

### Phase 1: Interactive Discovery & Creation

1.  **Mandatory Discovery Dialogue**: Before running `ng new`, present the detected CLI defaults (Phase 0) and ask for confirmation:
    - **Package Manager**: Detected or user-chosen (pnpm, npm, yarn)
    - **Styling**: `css` (or `scss`)
    - **SSR**: CLI default (detected in Phase 0)
    - **Routing**: CLI default (detected in Phase 0)
    - **Zoneless**: CLI default (detected in Phase 0)
    - **Git**: `enabled` (Required for Husky)
    - **RxJS Linting**: `auto` (default), `enabled`, or `disabled`
    - **Budget Profile**: `balanced` (default), `strict`, or `permissive`
    - **Environment files**: `optional`
2.  **Creation**: Once confirmed, execute the command. Use the flags derived from Phase 0 — only pass explicit flags for values that differ from the CLI defaults.
    - **Standard (CLI defaults)**: `ng new [project-name] --package-manager [pkg-manager] --style [style] --ssr [ssr-bool] --defaults`
    - **Override (e.g., opt out of zoneless)**: `ng new [project-name] --package-manager [pkg-manager] --style [style] --ssr [ssr-bool] --zoneless false --defaults`
3.  **Generate Project Manifest**: Write `PROJECT_MANIFEST.json` to the project root with all confirmed values from the discovery dialogue (see System Contract above). This manifest is consumed by all downstream sub-skills.
4.  **Structural Scaffolding**: Run `scripts/scaffold_structure.sh` to create the full `core/`, `shared/`, and `features/` hierarchy (including all sub-directories as defined in Pattern 1).
5.  **Environment files** (if requested): Run `ng generate environments` to generate the environment files.

### Phase 1.5: Production-Ready Setup Configuration

These configuration changes **cannot be scripted** because they modify complex JSON structures that require intelligent merging. The Agent handles them directly.

1. **TSConfig Path Aliases**: Add path aliases to `tsconfig.json` for clean imports:
   > **CAUTION:** Paths **must** use a leading `./` (relative) prefix. Without `baseUrl` set, TypeScript and Angular's esbuild builder will throw `TS5090: Non-relative paths are not allowed when 'baseUrl' is not set`.
   ```json
   {
     "compilerOptions": {
       "paths": {
         "@core/*": ["./src/app/core/*"],
         "@shared/*": ["./src/app/shared/*"],
         "@features/*": ["./src/app/features/*"]
       }
     }
   }
   ```
2. **Angular Budget Tuning**: Apply the `budgetProfile` confirmed during Interactive Discovery to `angular.json`. The profile values are:

   | Profile | `maximumWarning` | `maximumError` |
   |---------|-----------------|----------------|
   | `strict` | `500kB` | `1MB` |
   | `balanced` | `2MB` | `4MB` |
   | `permissive` | `4MB` | `8MB` |

   ```json
   {
     "budgets": [
       { "type": "initial", "maximumWarning": "[profile-warning]", "maximumError": "[profile-error]" },
       {
         "type": "anyComponentStyle",
         "maximumWarning": "10kB",
         "maximumError": "20kB"
       }
     ]
   }
   ```
   Tighten incrementally as you optimize bundle size over time.
3. **EditorConfig Alignment**: Verify `.editorconfig` aligns with Prettier defaults. Confirm:
   - `indent_style = space` and `indent_size = 2`
   - `insert_final_newline = true`
   - `charset = utf-8`
4. **README**: Replace the boilerplate README.md with a project-specific template:
   - Project name and description placeholder
   - Available NPM scripts table (`start`, `build`, `test`, `lint`, `lint:styles`, `format`)
   - Quality tools summary (ESLint, Stylelint, Prettier, Husky, Commitlint)
   - Conventional Commit format reference
   - Architecture overview (Feature-First, signals, facades, lazy routes)
   - License placeholder

### Phase 1.5 Verification

- Run `ng serve` and verify the application compiles and serves without errors or warnings.
- If warnings appear (e.g., path alias errors like `TS5090`), fix before proceeding to Phase 2.

### Phase 2: Autonomous Quality Layer (MANDATORY)

> **This phase is NOT optional.** Quality tooling is always set up regardless of user preferences.

Invoke the **`angular-linters`** skill to set up the complete quality stack (ESLint, Stylelint, Prettier, Husky, Commitlint).

The `angular-linters` skill will:

1. Run `scripts/configure_linters.sh --style [css|scss] --package-manager [pkg-manager] --rxjs [auto|enabled|disabled]` for automated installation. Pass the `--style`, `--package-manager`, and `--rxjs` values from `PROJECT_MANIFEST.json` so the correct dependencies are installed.
2. Verify all generated configuration files exist.
3. Incrementally add and verify ESLint plugins (Prettier → import-x/unused-imports → RxJS).
4. Validate Stylelint configuration.
5. Test Git hooks (Husky + Commitlint).
6. Perform final verification and cleanup.

**Critical**: Each configuration step in `angular-linters` must pass linting before proceeding to the next step.

### Phase 3: Third-Party Integration Transition

After the foundational project and the quality layer (`angular-linters`) are completely set up and validated, transition to third-party integrations:
1. Inform the user that the base Angular project setup is complete and production-ready.
2. Explicitly ask the user: *"Would you like to integrate any third-party libraries (e.g., TailwindCSS, Angular Material, Spartan UI, NgRx, Firebase)? If so, we will gracefully invoke the `angular-third-party-integration` skill to safely install them one by one. Please provide the list of libraries and their official installation documentation URLs."*
3. Once the user provides the list, invoke the **`angular-third-party-integration`** skill to handle the integrations.

## Resources

- **scripts/scaffold_structure.sh**: Deterministic folder hierarchy generator — creates the full `core/`, `shared/`, and `features/` tree.
- Phase 1.5 tasks (path aliases, budgets, editorconfig) are handled by the Agent, not scripts, because they require intelligent JSON merging.
- Architecture Patterns reference: see the **Angular 22 Architecture Patterns** section above.
