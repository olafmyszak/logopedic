import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { LoginDto } from '../models/loginDto';
import { environment } from '../../../environments/environment';
import { tap } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class AuthService {
    private http = inject(HttpClient);

    login(loginDto: LoginDto) {
        return this.http.post(`${environment.apiBaseUrl}/account/login`, loginDto, {
            withCredentials: true,
            // headers:
            //     {'X-XSRF-TOKEN': this.token!}
        });
    }

    logout() {
        return this.http.post(`${environment.apiBaseUrl}/account/logout`, null, {
            withCredentials: true
        });
    }

    private token: string | null = null;

    antiforgeryToken() {
        return this.http.get<{ token: string }>(`${environment.apiBaseUrl}/antiforgery/token`, {withCredentials: true})
            .pipe(tap(res => this.token = res.token
            ));
    }

    getToken() {
        return this.token;
    }
}
