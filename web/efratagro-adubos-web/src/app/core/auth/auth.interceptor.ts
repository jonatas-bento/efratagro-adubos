import {
  HttpInterceptorFn,
} from '@angular/common/http';
import {
  readAccessToken,
} from './auth-token.storage';

export const authInterceptor:
  HttpInterceptorFn =
    (request, next) => {
      const token =
        readAccessToken();

      if (
        !token ||
        !request.url.startsWith(
          '/api/',
        )
      ) {
        return next(request);
      }

      return next(
        request.clone({
          setHeaders: {
            Authorization:
              `Bearer ${token}`,
          },
        }),
      );
    };
