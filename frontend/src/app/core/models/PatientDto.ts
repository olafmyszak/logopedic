export interface PatientDto {
    id: number;
    fullName: string;
    dateOfBirth: Date;
    contactInfo: string;
    notes?: string;
}
