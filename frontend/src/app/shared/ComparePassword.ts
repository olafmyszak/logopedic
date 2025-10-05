import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export function ComparePassword(passwordKey: string, confirmPasswordKey: string): ValidatorFn {
    return (formGroup: AbstractControl): ValidationErrors | null => {
        const password = formGroup.get(passwordKey);
        const confirmPassword = formGroup.get(confirmPasswordKey);

        if (password?.value !== confirmPassword?.value) {
            return {mustMatch: true};
        }

        return null;
    };
}
