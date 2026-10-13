import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';

import { routes } from './app.routes';
import { userIdInterceptor } from './core/interceptors/user-id.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withInterceptors([userIdInterceptor])),
    // Lets route parameters (like :roomId) be received as component inputs.
    provideRouter(routes, withComponentInputBinding()),
  ],
};
