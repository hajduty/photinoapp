export interface GetHeatmapResponse {
    Heatmaps: HeatmapData[]
}

export interface HeatmapData {
    Date: string,
    Applications: number
}