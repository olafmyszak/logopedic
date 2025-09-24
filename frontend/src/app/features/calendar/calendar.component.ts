import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import {
    CalendarDateFormatter,
    CalendarDatePipe,
    CalendarEvent,
    CalendarEventTimesChangedEvent,
    CalendarEventTitleFormatter,
    CalendarWeekViewComponent,
    DateAdapter,
    DAYS_OF_WEEK,
    provideCalendar
} from 'angular-calendar';
import { adapterFactory } from 'angular-calendar/date-adapters/date-fns';
import { CalendarIntegrationService } from '../../core/services/calendar-integration.service';
import { endOfWeek, startOfWeek } from 'date-fns';
import { registerLocaleData } from '@angular/common';
import localePl from '@angular/common/locales/pl';
import { CustomDateFormatter } from './custom-date-formatter.provider';
import { CustomEventTitleFormatter } from './custom-event-title-formatter.provider';

registerLocaleData(localePl);

@Component({
    selector: 'app-calendar',
    imports: [CalendarWeekViewComponent, CalendarDatePipe],
    providers: [
        provideCalendar({
                provide: DateAdapter,
                useFactory: adapterFactory
            },
            {
                dateFormatter: {
                    provide: CalendarDateFormatter,
                    useClass: CustomDateFormatter
                },
                eventTitleFormatter: {
                    provide: CalendarEventTitleFormatter,
                    useClass: CustomEventTitleFormatter
                }
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
    readonly locale: string = 'pl';
    readonly weekStartsOn: number = DAYS_OF_WEEK.MONDAY;

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
