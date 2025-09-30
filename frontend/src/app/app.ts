import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
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
    protected readonly title = signal('frontend');
    private auth = inject(AuthService);
    private _isLoggedIn = toSignal(this.auth.isLoggedIn(), {initialValue: false});
    isLoggedIn = computed(() => this._isLoggedIn());

    ngOnInit(): void {
        this.auth.antiforgeryToken().subscribe();
    }

    logout() {
        this.auth.logout().subscribe(() => {
            window.location.reload();
        });
    }
}
