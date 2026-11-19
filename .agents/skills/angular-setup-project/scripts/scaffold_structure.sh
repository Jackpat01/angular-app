#!/bin/bash

# Standard Angular Folder Scaffolding Script
# Creates the full Feature-First hierarchy aligned with Angular 22 architecture patterns.
# See SKILL.md → "Angular 22 Architecture Patterns" for the rationale behind each folder.

PROJECT_ROOT=${1:-"."}

echo "🚀 Scaffolding Angular directory structure in: $PROJECT_ROOT"

DIRS=(
  # core/ — cross-cutting concerns shared by ALL features
  #   auth/         → AuthService, tokens, interceptors
  #   guards/       → app-wide functional CanActivateFn guards
  #   interceptors/ → HttpInterceptorFn implementations
  #   models/       → shared interfaces/types (ARUM, Classification, etc.)
  #   services/     → shared singleton services (API, session, portal config)
  "src/app/core/auth"
  "src/app/core/guards"
  "src/app/core/interceptors"
  "src/app/core/models"
  "src/app/core/services"

  # shared/ — reusable UI with zero business logic
  #   base/         → abstract base component classes (BaseFormComponent, etc.)
  #   components/   → pure presentational components (DataTable, ErrorDisplay, etc.)
  "src/app/shared/base"
  "src/app/shared/components"

  # features/ — one subfolder per business feature (added as app grows)
  # Each feature owns: models/, services/, pages/, optionally guards/ + facades/
  "src/app/features"
)

for dir in "${DIRS[@]}"; do
  full_path="$PROJECT_ROOT/$dir"
  if [ ! -d "$full_path" ]; then
    mkdir -p "$full_path"
    echo "✅ Created: $dir"
  else
    echo "ℹ️  Skipped: $dir (already exists)"
  fi
done

echo ""
echo "📐 Architecture overview:"
echo "  core/auth/         → AuthService, interceptors"
echo "  core/guards/       → app-wide functional guards"
echo "  core/interceptors/ → HttpInterceptorFn implementations"
echo "  core/models/       → shared interfaces / types"
echo "  core/services/     → shared singleton services"
echo "  shared/base/       → abstract base component classes"
echo "  shared/components/ → pure presentational UI components"
echo "  features/          → one subfolder per feature (add as needed)"
echo ""
echo "📏 When adding a file, ask: does it belong to ONE feature or MULTIPLE?"
echo "   One feature  → features/[name]/"
echo "   Multiple     → core/ (services/models) or shared/ (UI)"
echo ""
echo "🎉 Scaffolding complete!"

