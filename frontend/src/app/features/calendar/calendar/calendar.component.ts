import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import {
    CalendarDatePipe,
    CalendarEvent,
    CalendarEventTimesChangedEvent,
    CalendarWeekViewComponent,
    DateAdapter,
    provideCalendar
} from 'angular-calendar';
import { adapterFactory } from 'angular-calendar/date-adapters/date-fns';
import { CalendarIntegrationService } from '../../../core/services/calendar-integration.service';
import { endOfWeek, startOfWeek } from 'date-fns';

@Component({
    selector: 'app-calendar',
    imports: [CalendarWeekViewComponent, CalendarDatePipe],
    providers: [
        provideCalendar({
            provide: DateAdapter,
            useFactory: adapterFactory
        })
    ],
    templateUrl: './calendar.component.html',
    styleUrl: './calendar.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class CalendarComponent implements OnInit {
    private calendarIntegration = inject(CalendarIntegrationService);

    readonly events = this.calendarIntegration.events;
    readonly loading = this.calendarIntegration.loading;
    readonly error = this.calendarIntegration.error;
    readonly refresh = this.calendarIntegration.refresh;

    viewDate = new Date();
    locale = "pl"

    ngOnInit() {
        this.loadCurrentWeek();
        console.log(this.locale)
    }

    previousWeek() {
        const d = new Date(this.viewDate);
        d.setDate(d.getDate() - 7);
        this.viewDate = d;
        this.loadCurrentWeek();
    }

    nextWeek() {
        const d = new Date(this.viewDate);
        d.setDate(d.getDate() + 7);
        this.viewDate = d;
        this.loadCurrentWeek();
    }


    onEventClicked(ev: CalendarEvent) {
        console.log('clicked', ev.meta);
    }

    onEventTimesChanged({
                            event,
                            newStart,
                            newEnd
                        }: CalendarEventTimesChangedEvent) {
        this.events.update(list =>
            list.map(e => (e === event ? {...e, start: newStart, end: newEnd} : e))
        );
    }

    private loadCurrentWeek() {
        const start = startOfWeek(this.viewDate, {weekStartsOn: 1});
        const end = endOfWeek(this.viewDate, {weekStartsOn: 1});
        this.calendarIntegration.loadRange(start, end);
    }
}


// private appointmentsData = toSignal(
//     toObservable(this.queryParams).pipe(
//         tap(() => {
//             this.loading.set(true);
//             this.error.set(null);
//         }),
//         switchMap(params => {
//             return this.appointmentsService.query(params).pipe(
//                 catchError(err => {
//                     console.error('Appointments query failed', err);
//                     this.error.set('Failed to load appointments');
//                     return of(emptyPagedResult<AppointmentDto>());
//                 }),
//                 finalize(() => {
//                     this.loading.set(false);
//                 })
//             );
//         })
//     ),
//     {
//         initialValue: emptyPagedResult<AppointmentDto>()
//     }
// );
//
// appointments = computed(() => this.appointmentsData().items);

