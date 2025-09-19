import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
    let cloned = req.clone({withCredentials: true});

    const auth = inject(AuthService);
    const token = auth.getToken();

    // Manually add XSRF token header because Angular's automatic way doesn't work
    if (token) {
        cloned = cloned.clone({
            setHeaders:
                {'X-XSRF-TOKEN': token}
        });
    }

    console.log(token);

    return next(cloned);
};
