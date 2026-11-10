import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { BaseLayout } from './app/layouts/base-layout/base-layout';

bootstrapApplication(BaseLayout, appConfig)
  .catch((err) => console.error(err));

