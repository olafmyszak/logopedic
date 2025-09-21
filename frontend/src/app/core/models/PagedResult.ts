export interface PagedResult<T> {
    items: T[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
}

export function emptyPagedResult<T>(): PagedResult<T> {
    return {
        items: [],
        totalCount: 0,
        pageNumber: 1,
        pageSize: 20
    }
}
