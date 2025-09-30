import { AppointmentType } from './AppointmentType';
import { AppointmentStatus } from './AppointmentStatus';

export interface AppointmentDto {
    id: number;
    patientId: number;
    startTime: Date;
    durationInMinutes: number;
    type: AppointmentType;
    status: AppointmentStatus;
}
