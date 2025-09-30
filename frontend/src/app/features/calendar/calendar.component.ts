import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import {
    CalendarDateFormatter,
    CalendarDatePipe,
    CalendarEvent,
    CalendarEventTimesChangedEvent,
    CalendarWeekViewComponent,
    DateAdapter,
    DAYS_OF_WEEK,
    provideCalendar
} from 'angular-calendar';
import { adapterFactory } from 'angular-calendar/date-adapters/date-fns';
import { CalendarIntegrationService } from '../../core/services/calendar-integration.service';
import { endOfWeek, isSameDay, startOfWeek } from 'date-fns';
import { DatePipe, registerLocaleData } from '@angular/common';
import localePl from '@angular/common/locales/pl';
import { CustomDateFormatter } from './custom-date-formatter.provider';

registerLocaleData(localePl);

@Component({
    selector: 'app-calendar',
    imports: [CalendarWeekViewComponent, CalendarDatePipe, DatePipe],
    providers: [
        provideCalendar({
                provide: DateAdapter,
                useFactory: adapterFactory
            },
            {
                dateFormatter: {
                    provide: CalendarDateFormatter,
                    useClass: CustomDateFormatter
                }
            })
    ],
    templateUrl: './calendar.component.html',
    styleUrl: './calendar.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class CalendarComponent implements OnInit {
    viewDate: Date = new Date();
    readonly locale: string = 'pl';
    readonly weekStartsOn: number = DAYS_OF_WEEK.MONDAY;
    private calendarIntegration = inject(CalendarIntegrationService);
    readonly events = this.calendarIntegration.events;
    readonly loading = this.calendarIntegration.loading;
    readonly error = this.calendarIntegration.error;
    readonly refresh = this.calendarIntegration.refresh;

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
        if (this.validateEventTimesChanged(changedEvent)) {
            this.calendarIntegration.update(changedEvent);
        }
    }

    validateEventTimesChanged = (
        {event, newStart, newEnd}: CalendarEventTimesChangedEvent,
        addCssClass = true
    ): boolean => {
        delete event.cssClass;

        const sameDay = isSameDay(newStart, newEnd!);

        if (!sameDay) {
            return true;
        }

        const overlappingEvent = this.events().find((otherEvent) => {
            return (
                otherEvent !== event &&
                (otherEvent.start < newEnd! && otherEvent.end! > newStart)
            );
        });

        if (overlappingEvent) {
            if (addCssClass) {
                event.cssClass = 'invalid-position';
            }

            return false;
        }

        return true;
    };

    private loadCurrentWeek() {
        const start = startOfWeek(this.viewDate, {weekStartsOn: 1});
        const end = endOfWeek(this.viewDate, {weekStartsOn: 1});
        this.calendarIntegration.loadRange(start, end);
    }
}
