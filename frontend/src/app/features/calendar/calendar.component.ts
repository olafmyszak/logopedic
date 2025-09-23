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
import { CalendarIntegrationService } from '../../core/services/calendar-integration.service';
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

    viewDate = new Date();
    readonly locale = 'pl';
    readonly weekStartsOn = 1; // Start on Monday

    ngOnInit() {
        this.loadCurrentWeek();
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
        console.log('clicked', ev);
    }

    onEventTimesChanged(changedEvent: CalendarEventTimesChangedEvent) {
        this.calendarIntegration.update(changedEvent);
    }

    private loadCurrentWeek() {
        const start = startOfWeek(this.viewDate, {weekStartsOn: 1});
        const end = endOfWeek(this.viewDate, {weekStartsOn: 1});
        this.calendarIntegration.loadRange(start, end);
    }
}
