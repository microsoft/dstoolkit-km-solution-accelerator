export enum FacetType {
    Tags = "tags",
    Industries = "industries",
    // AssetTypes = "assetType",
}
export interface Facet {
    name: FacetType;
    categories: Record<string, number>;
    totalResults?: number;
}