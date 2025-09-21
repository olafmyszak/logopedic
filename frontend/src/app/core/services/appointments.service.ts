import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { AppointmentQueryParams } from '../models/AppointmentQueryParams';
import { PagedResult } from '../models/PagedResult';
import { AppointmentDto } from '../models/AppointmentDto';
import { environment } from '../../../environments/environment';

@Injectable({
    providedIn: 'root'
})
export class AppointmentsService {
    private http = inject(HttpClient);

    query(params: AppointmentQueryParams) {
        const httpParams = this.buildHttpParams(params);
        return this.http.get<PagedResult<AppointmentDto>>(`${environment.apiBaseUrl}/appointments`, {
            params: httpParams
        });
    }

    private buildHttpParams(params: AppointmentQueryParams): HttpParams {
        let httpParams = new HttpParams();

        if (params.from !== undefined) {
            httpParams = httpParams.set('From', params.from.toISOString());
        }

        if (params.to !== undefined) {
            httpParams = httpParams.set('To', params.to.toISOString());
        }

        if (params.pageNumber !== undefined) {
            httpParams = httpParams.set('PageNumber', params.pageNumber.toString());
        }

        if (params.pageSize !== undefined) {
            httpParams = httpParams.set('PageSize', params.pageSize.toString());
        }

        if (params.sort !== undefined) {
            httpParams = httpParams.set('Sort', params.sort);
        }

        if (params.patientId !== undefined) {
            httpParams = httpParams.set('PatientId', params.patientId.toString());
        }

        if (params.status && params.status.length > 0) {
            params.status.forEach(status => {
                httpParams = httpParams.append('Status', status);
            });
        }

        if (params.type && params.type.length > 0) {
            params.type.forEach(type => {
                httpParams = httpParams.append('Type', type);
            });
        }

        return httpParams;
    }
}
