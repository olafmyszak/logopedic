export interface PatientQueryParams {
    //Paging
    pageNumber?: number;
    pageSize?: number;

    // Sorting: comma-separated "field:dir" pairs, e.g. "fullName:asc,id:desc"
    sort?: string;

    // Search by patient's name and contact info
    search?: string;
}
