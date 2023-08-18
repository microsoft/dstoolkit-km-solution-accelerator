import express from "express";
import bodyParser from "body-parser";
import cors from "cors";

/* Setup server */
const app = express();
app.use(
    cors({
        origin: "*",
        exposedHeaders: ["Content-Disposition", "x-continuation-token"],
    })
);
app.use(bodyParser.json());
app.use(bodyParser.urlencoded({ extended: true }));

/* Mocks */
import { mockFacets } from "./mockFacets.js";
app.get("/api/facets", (_req, res) => {
    setTimeout(() => {
        res.json(mockFacets);
    }, 2000);
});

/* Run server */
const port = 5901;
app.listen(port, () => {
    return console.log(`http://localhost:${port}`);
});
