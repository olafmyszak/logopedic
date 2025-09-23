import { AppointmentType } from './AppointmentType';
import { AppointmentStatus } from './AppointmentStatus';

export interface PatchAppointmentDto {
    startTime?: Date;
    durationInMinutes?: number;
    type?: AppointmentType;
    status?: AppointmentStatus;
    patientId?: number;
}
