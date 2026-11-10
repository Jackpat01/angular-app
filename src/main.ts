import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { PublicLayout } from './app/layouts/public-layout/public-layout';

bootstrapApplication(PublicLayout, appConfig)
  .catch((err) => console.error(err));

