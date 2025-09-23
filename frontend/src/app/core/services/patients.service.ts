import { HttpClient, HttpParams } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { PatientQueryParams } from '../models/PatientQueryParams';
import { PagedResult } from '../models/PagedResult';
import { PatientDto } from '../models/PatientDto';
import { environment } from '../../../environments/environment';
import { catchError, finalize, Observable, of, share, Subscription, switchMap, tap } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class PatientsService {
    private http = inject(HttpClient);

    private loadSubscription?: Subscription;

    readonly patients = signal<PatientDto[]>([]);
    readonly loading = signal(false);
    readonly error = signal<string | null>(null);
    readonly patientsMap = computed(() => {
        const map = new Map<number, PatientDto>();
        this.patients().forEach(patient => {
            map.set(patient.id, patient);
        });
        return map;
    });

    query(params: PatientQueryParams) {
        const httpParams = this.buildHttpParams(params);

        return this.http.get<PagedResult<PatientDto>>(`${environment.apiBaseUrl}/patients`, {
            params: httpParams
        });
    }

    getById(id: number) {
        return this.http.get<PatientDto>(`${environment.apiBaseUrl}/patients/${id}`);
    }

    loadAll(): Observable<PatientDto[]> {
        this.loadSubscription?.unsubscribe();

        this.loading.set(false);
        this.error.set(null);

        const result$ = this.loadAllRecursive(1, []).pipe(
            tap(patients => this.patients.set(patients)),
            catchError(err => {
                this.error.set(err?.message ?? 'Failed to load patients');
                return of<PatientDto[]>([]);
            }),
            finalize(() => {
                this.loading.set(false);
                this.loadSubscription = undefined;
            }),
            share()
        );

        this.loadSubscription = result$.subscribe();
        return result$;
    }

    private loadAllRecursive(pageNumber: number, accumulator: PatientDto[]): Observable<PatientDto[]> {
        const params: PatientQueryParams = {
            pageNumber: pageNumber,
            pageSize: PatientQueryParams.maxPageSize
        };

        return this.query(params).pipe(
            switchMap(result => {
                const allPatients = [...accumulator, ...result.items];

                const totalPages = Math.ceil(result.totalCount / result.pageSize);
                const hasMorePages = pageNumber < totalPages;

                if (hasMorePages) {
                    return this.loadAllRecursive(pageNumber + 1, allPatients);
                }

                return of(allPatients);
            })
        );
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
