import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { PatientQueryParams } from '../models/PatientQueryParams';
import { PagedResult } from '../models/PagedResult';
import { PatientDto } from '../models/PatientDto';
import { environment } from '../../../environments/environment';

@Injectable({
    providedIn: 'root'
})
export class PatientsService {
    private http = inject(HttpClient);

    query(params: PatientQueryParams) {
        const httpParams = this.buildHttpParams(params);

        return this.http.get<PagedResult<PatientDto>>(`${environment.apiBaseUrl}/patients`, {
            params: httpParams
        });
    }

    getById(id: number) {
        return this.http.get<PatientDto>(`${environment.apiBaseUrl}/patients/${id}`);
    }


    private buildHttpParams(params: PatientQueryParams): HttpParams {
        let httpParams = new HttpParams();

        if (params.pageNumber !== undefined) {
            httpParams = httpParams.set('PageNumber', params.pageNumber.toString());
        }

        if (params.pageSize !== undefined) {
            httpParams = httpParams.set('PageSize', params.pageSize.toString());
        }

        if (params.sort !== undefined) {
            httpParams = httpParams.set('Sort', params.sort);
        }

        if (params.search !== undefined) {
            httpParams = httpParams.set('Search', params.search);
        }

        return httpParams;
    }
}
