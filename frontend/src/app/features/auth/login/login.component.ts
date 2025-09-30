import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { Router } from '@angular/router';
import { LoginDto } from '../../../core/models/LoginDto';
import { finalize } from 'rxjs';

@Component({
    selector: 'app-login',
    imports: [
        ReactiveFormsModule
    ],
    templateUrl: './login.component.html',
    styleUrl: './login.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginComponent {
    serverError = signal<string | null>(null);
    isLoading = signal(false);
    private fb = inject(FormBuilder);
    form = this.fb.group({
        email: ['', [Validators.required, Validators.email]],
        password: ['', Validators.required],
        rememberMe: [true]
    });
    private auth = inject(AuthService);
    private router = inject(Router);

    emailError(): string | null {
        const ctl = this.form.get('email');

        if (!ctl || !ctl.touched) {
            return null;
        }

        if (ctl.hasError('required')) {
            return 'Email is required';
        }

        if (ctl.hasError('email')) {
            return 'Enter a valid email';
        }

        return null;
    }

    passwordError(): string | null {
        const ctl = this.form.get('password');

        if (!ctl || !ctl.touched) {
            return null;
        }

        if (ctl.hasError('required')) {
            return 'Email is required';
        }

        return null;
    }

    onSubmit() {
        this.serverError.set(null);

        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }

        this.isLoading.set(true);

        const loginDto: LoginDto = {
            email: this.form.value.email!,
            password: this.form.value.password!,
            isPersistent: this.form.value.rememberMe!
        };

        this.auth.login(loginDto)
            .pipe(finalize(() => this.isLoading.set(false)))
            .subscribe({
                next: () => this.router.navigateByUrl('/').then(() => window.location.reload()),
                error: (err) => {
                    const msg = err?.error?.message ?? err?.message ?? 'Unable to sign in';
                    this.serverError.set(msg);
                }
            });
    }
}
