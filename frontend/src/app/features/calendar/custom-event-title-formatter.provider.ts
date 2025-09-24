import { inject, Injectable, LOCALE_ID } from '@angular/core';
import { CalendarEvent, CalendarEventTitleFormatter } from 'angular-calendar';
import { formatDate } from '@angular/common';

@Injectable()
export class CustomEventTitleFormatter extends CalendarEventTitleFormatter {
    private locale = inject(LOCALE_ID);

    override week(event: CalendarEvent, title: string): string {
        return `<b>${formatDate(event.start, 'HH:mm', this.locale)} - ${formatDate(event.end!, 'HH:mm', this.locale)}</b> ${
            title
        }`;
    }
}
