import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from './core/services/auth.service';
import { toSignal } from '@angular/core/rxjs-interop';

@Component({
    selector: 'app-root',
    imports: [
        RouterOutlet,
        RouterLink
    ],
    templateUrl: './app.html',
    styleUrl: './app.scss'
})
export class App implements OnInit {
    private auth = inject(AuthService);

    ngOnInit(): void {
        this.auth.antiforgeryToken().subscribe();
    }

    private _isLoggedIn = toSignal(this.auth.isLoggedIn(), {initialValue: false});
    isLoggedIn = computed(() => this._isLoggedIn());

    protected readonly title = signal('frontend');

    logout() {
        this.auth.logout().subscribe(() => {
            window.location.reload();
        });
    }
}
