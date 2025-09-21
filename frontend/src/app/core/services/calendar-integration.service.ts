import { inject, Injectable, signal } from '@angular/core';
import { AppointmentsService } from './appointments.service';
import { CalendarEvent } from 'angular-calendar';
import { catchError, map, of, Subject } from 'rxjs';
import { AppointmentQueryParams } from '../models/AppointmentQueryParams';
import { AppointmentDto } from '../models/AppointmentDto';
import { PagedResult } from '../models/PagedResult';

@Injectable({
    providedIn: 'root'
})
export class CalendarIntegrationService {
    private appointmentService = inject(AppointmentsService);

    readonly events = signal<CalendarEvent[]>([]);
    readonly loading = signal(false);
    readonly error = signal<string | null>(null);

    readonly refresh = new Subject<void>();
    readonly locale = signal("us");

    loadRange(from: Date, to: Date) {
        this.loading.set(true);
        this.error.set(null);

        const params: AppointmentQueryParams = {
            from: from,
            to: to,
            pageNumber: 1,
            pageSize: 20
        };

        this.appointmentService.query(params).pipe(
            map((res: PagedResult<AppointmentDto>) =>
                res.items.map(dto => this.mapDtoToEvent(dto))
            ),
            catchError(err => {
                this.error.set(err?.message ?? 'Failed to load appointments');
                return of<CalendarEvent[]>([]);
            })
        ).subscribe(events => {
            this.events.set(events);
            this.loading.set(false);
            this.refresh.next();
        });
    }

    private mapDtoToEvent(dto: AppointmentDto): CalendarEvent {
        const start: Date = new Date(dto.startTime);
        const end = new Date(start.getTime() + dto.durationInMinutes * 60 * 1000);

        const primary = '#1976d2';
        const secondary = '#BBDEFB';

        return {
            id: dto.id,
            title: `Appointment #${dto.id}`,
            start,
            end,
            color: {primary, secondary},
            draggable: true,
            resizable: {
                beforeStart: true,
                afterEnd: true
            },
            meta: dto
        };
    }
}
