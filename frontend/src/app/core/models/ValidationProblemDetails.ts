export interface ValidationProblemDetails {
    type: string;
    title: string;
    status: number;
    errors: { [key: string]: string[] };
    traceId?: string;
}
