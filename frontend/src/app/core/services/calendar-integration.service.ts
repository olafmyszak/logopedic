import { inject, Injectable, signal } from '@angular/core';
import { AppointmentsService } from './appointments.service';
import { CalendarEvent, CalendarEventTimesChangedEvent } from 'angular-calendar';
import { catchError, combineLatest, finalize, map, of, Subject, Subscription } from 'rxjs';
import { AppointmentQueryParams } from '../models/AppointmentQueryParams';
import { AppointmentDto } from '../models/AppointmentDto';
import { emptyPagedResult, PagedResult } from '../models/PagedResult';
import { PatientsService } from './patients.service';
import { PatchAppointmentDto } from '../models/PatchAppointmentDto';
import { PatientDto } from '../models/PatientDto';

@Injectable({
    providedIn: 'root'
})
export class CalendarIntegrationService {
    readonly events = signal<CalendarEvent[]>([]);
    readonly loading = signal(false);
    readonly error = signal<string | null>(null);
    refresh = new Subject<void>();
    private appointmentService = inject(AppointmentsService);
    private patientsService = inject(PatientsService);
    private currentLoadSubscription?: Subscription;

    loadRange(from: Date, to: Date) {
        this.currentLoadSubscription?.unsubscribe();

        this.loading.set(true);
        this.error.set(null);

        const patients$ = this.patientsService.loadAll();

        const params: AppointmentQueryParams = {
            from,
            to,
            pageNumber: 1,
            pageSize: AppointmentQueryParams.maxPageSize
        };

        const appointments$ = this.appointmentService.query(params).pipe(
            catchError(err => {
                console.error('Failed to load appointments', err);
                this.error.set(err?.message ?? 'Failed to load appointments');
                return of<PagedResult<AppointmentDto>>(emptyPagedResult());
            })
        );

        this.currentLoadSubscription = combineLatest([patients$, appointments$]).pipe(
            map(([_patients, appointments]) => {
                const patientsMap = this.patientsService.patientsMap();
                return appointments.items.map(dto => this.mapDtoToEvent(dto, patientsMap));
            }),
            catchError(err => {
                console.error(err);
                this.error.set(err?.message ?? 'Failed to load calendar data');
                return of<CalendarEvent[]>([]);
            }),
            finalize(() => {
                this.loading.set(false);
                this.currentLoadSubscription = undefined;
            })
        ).subscribe(events =>
            this.events.set(events)
        );
    }

    update(changedEvent: CalendarEventTimesChangedEvent) {
        this.loading.set(true);
        this.error.set(null);

        const id = Number(changedEvent.event.id);

        if (isNaN(id)) {
            this.loading.set(false);
            this.error.set('Appointment id is NaN');
            console.error('Appointment id is NaN', changedEvent.event.id);
            return;
        }

        const dto: PatchAppointmentDto = {
            startTime: changedEvent.newStart
        };

        this.appointmentService.patch(id, dto).subscribe({
            next: () => {
                this.events.update(events =>
                    events.map(event =>
                        event.id === id
                            ? {...event, start: changedEvent.newStart, end: changedEvent.newEnd}
                            : event
                    )
                );
                this.loading.set(false);
                this.refresh.next();
            },
            error: err => {
                this.error.set('Failed to update appointment');
                this.loading.set(false);
                console.error('Patch appointment error:', err);
            }
        });
    }

    private mapDtoToEvent(dto: AppointmentDto, patientsMap: Map<number, PatientDto>): CalendarEvent {
        const start: Date = new Date(dto.startTime);
        const end = new Date(start.getTime() + dto.durationInMinutes * 60 * 1000);

        const primary = '#1976d2';
        const secondary = '#BBDEFB';

        const title = patientsMap.get(dto.patientId)?.fullName ?? '';

        return {
            id: dto.id,
            title: title,
            start: start,
            end: end,
            color: {primary, secondary},
            draggable: true,
            resizable: {
                beforeStart: true,
                afterEnd: true
            },
            cssClass: dto.durationInMinutes <= 30 ? 'short-event' : '',
            meta: dto
        };
    }
}
