export interface GetHeatmapDateResponse {
    Jobs: HeatmapJobData[]
}

export interface HeatmapJobData {
    JobId: number,
    JobTitle: string,
    Company: string,
    CompanyImage: string
}