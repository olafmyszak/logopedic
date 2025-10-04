import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { Router } from '@angular/router';
import { LoginDto } from '../../../core/models/LoginDto';
import { finalize } from 'rxjs';
import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { ValidationProblemDetails } from '../../../core/models/ValidationProblemDetails';

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
    private fb = inject(FormBuilder);
    private auth = inject(AuthService);
    private router = inject(Router);

    form = this.fb.group({
        email: ['', [Validators.required, Validators.email]],
        password: ['', Validators.required],
        rememberMe: [true]
    });

    serverError = signal<string | null>(null);
    isLoading = signal(false);

    emailError(): string | null {
        const ctl = this.form.get('email');

        if (!ctl || !ctl.touched) {
            return null;
        }

        if (ctl.hasError('required')) {
            return 'Email is required.';
        }

        if (ctl.hasError('email')) {
            return 'Enter a valid email.';
        }

        return null;
    }

    passwordError(): string | null {
        const ctl = this.form.get('password');

        if (!ctl || !ctl.touched) {
            return null;
        }

        if (ctl.hasError('required')) {
            return 'Email is required.';
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
                error: (err: HttpErrorResponse) => {
                    if (err.status === HttpStatusCode.BadRequest && err.error) {
                        const problemDetails = err.error as ValidationProblemDetails;

                        const allErrors: string = Object.values(problemDetails.errors)
                            .flat()
                            .join(', ');

                        this.serverError.set(allErrors);
                    } else if (err.status == HttpStatusCode.Unauthorized) {
                        this.serverError.set('Invalid username or password.');
                    }
                }
            });
    }
}
