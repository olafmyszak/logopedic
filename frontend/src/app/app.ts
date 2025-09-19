import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from './core/services/auth.service';

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

    protected readonly title = signal('frontend');
}
