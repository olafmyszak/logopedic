/// <reference types="@angular/localize" />

import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

import pl from '@angular/common/locales/pl';
import { registerLocaleData } from '@angular/common';

registerLocaleData(pl);

bootstrapApplication(App, appConfig)
    .catch((err) => console.error(err));
