import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ComparePassword } from '../../../shared/ComparePassword';
import { AuthService } from '../../../core/services/auth.service';
import { Router } from '@angular/router';
import { RegisterDto } from '../../../core/models/RegisterDto';
import { ValidationProblemDetails } from '../../../core/models/ValidationProblemDetails';
import { finalize } from 'rxjs';
import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';

@Component({
    selector: 'app-register',
    imports: [
        ReactiveFormsModule
    ],
    templateUrl: './register.component.html',
    styleUrl: './register.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class RegisterComponent {
    private fb = inject(FormBuilder);
    private auth = inject(AuthService);
    private router = inject(Router);

    private readonly MIN_PASSWORD_LENGTH = 8;
    form = this.fb.nonNullable.group({
            email: ['', [Validators.required, Validators.email]],
            password: ['', [Validators.required, Validators.minLength(this.MIN_PASSWORD_LENGTH)]],
            confirmPassword: ['', [Validators.required]],
            fullName: ['', [Validators.required]]
        },
        {
            validators: ComparePassword('password', 'confirmPassword')
        }
    );

    serverError = signal<string | null>(null);
    isLoading = signal(false);

    fullNameError(): string | null {
        const ctl = this.form.get('fullName');

        if (!ctl || !ctl.touched) {
            return null;
        }

        if (ctl.hasError('required')) {
            return 'Name is required.';
        }

        return null;
    }

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
            return 'Password is required.';
        }

        if (ctl.hasError('minlength')) {
            return `Password needs to be at least ${this.MIN_PASSWORD_LENGTH} characters long.`;
        }

        return null;
    }

    confirmPasswordError(): string | null {
        const ctl = this.form.get('confirmPassword');

        if (!ctl || !ctl.touched) {
            return null;
        }

        if (ctl.hasError('required')) {
            return 'Please confirm your password.';
        }

        if (ctl.hasError('mustMatch')) {
            return 'Passwords must match.';
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

        const {email, password, fullName} = this.form.getRawValue();
        const registerDto: RegisterDto = {
            email,
            password,
            fullName: fullName.trim()
        };

        this.auth.register(registerDto)
            .pipe(finalize(() => this.isLoading.set(false)))
            .subscribe({
                next: () => this.router.navigateByUrl('/').then(() => window.location.reload()),
                error: (err: HttpErrorResponse) => {
                    if (err.status === HttpStatusCode.BadRequest && err.error) {
                        const problemDetails = err.error as ValidationProblemDetails;

                        if (problemDetails.errors['DuplicateEmail']) {
                            this.serverError.set(problemDetails.errors['DuplicateEmail'][0]);
                        } else {

                            const allErrors: string = Object.values(problemDetails.errors)
                                .flat()
                                .join(', ');

                            this.serverError.set(allErrors);
                        }
                    }
                }
            });
    }
}
