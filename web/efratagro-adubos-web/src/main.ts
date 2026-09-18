import {
  registerLocaleData,
} from '@angular/common';
import localePt from '@angular/common/locales/pt';
import {
  bootstrapApplication,
} from '@angular/platform-browser';

import { App } from './app/app';
import {
  appConfig,
} from './app/app.config';

registerLocaleData(localePt);

bootstrapApplication(
  App,
  appConfig,
).catch(error =>
  console.error(error),
);
