import { AppointmentStatus } from './AppointmentStatus';
import { AppointmentType } from './AppointmentType';

export interface AppointmentQueryParams {
    // Date range - using Date objects (will be serialized to ISO strings when sent to API)
    from?: Date;
    to?: Date;

    // Paging
    pageNumber?: number;
    pageSize?: number;

    // Sorting: comma-separated "field:dir" pairs, e.g. "startTime:asc,id:desc"
    sort?: string;

    // Filtering
    patientId?: number;

    // Arrays for multiple values (will be sent as repeated query params)
    status?: AppointmentStatus[];
    type?: AppointmentType[];
}
