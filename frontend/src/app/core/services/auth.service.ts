import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { LoginDto } from '../models/LoginDto';
import { environment } from '../../../environments/environment';
import { catchError, map, of, tap } from 'rxjs';
import { RegisterDto } from '../models/RegisterDto';

@Injectable({
    providedIn: 'root'
})
export class AuthService {
    private http = inject(HttpClient);

    private token: string | null = null;

    register(registerDto: RegisterDto) {
        return this.http.post(`${environment.apiBaseUrl}/account/register`, registerDto, {
            withCredentials: true
        });
    }

    login(loginDto: LoginDto) {
        return this.http.post(`${environment.apiBaseUrl}/account/login`, loginDto, {
            withCredentials: true
        });
    }

    logout() {
        return this.http.post(`${environment.apiBaseUrl}/account/logout`, null, {
            withCredentials: true
        });
    }

    isLoggedIn() {
        return this.http.get<boolean>(`${environment.apiBaseUrl}/account/isLoggedIn`, {withCredentials: true})
            .pipe(
                map(() => true),
                catchError(() => of(false))
            );
    }

    antiforgeryToken() {
        return this.http.get<{ token: string }>(`${environment.apiBaseUrl}/antiforgery/token`, {withCredentials: true})
            .pipe(tap(res => this.token = res.token
            ));
    }

    getToken() {
        return this.token;
    }
}
