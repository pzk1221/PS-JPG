#target photoshop

(function () {
    function fail(message) {
        return "Failed: " + message;
    }

    if (app.documents.length === 0) {
        return fail("Open a document in Photoshop first.");
    }

    var doc = app.activeDocument;
    var outputFolder;

    try {
        outputFolder = doc.path;
    } catch (pathErr) {
        return fail("The current document has no source folder. Open a file from disk or save it first.");
    }

    var originalRulerUnits = app.preferences.rulerUnits;
    app.preferences.rulerUnits = Units.PIXELS;

    var allLayers = [];
    var exportLayers = [];
    var parentMap = {};

    function layerId(layer) {
        return String(layer.id);
    }

    function isBackgroundLayer(layer) {
        try {
            if (doc.backgroundLayer && layer === doc.backgroundLayer) {
                return true;
            }
        } catch (e) {}

        var n = String(layer.name).toLowerCase();
        return n === "background";
    }

    function collect(container, parents) {
        for (var i = 0; i < container.layers.length; i++) {
            var layer = container.layers[i];
            allLayers.push(layer);
            parentMap[layerId(layer)] = parents.slice(0);

            if (layer.typename === "LayerSet") {
                collect(layer, parents.concat([layer]));
            } else if (!isBackgroundLayer(layer)) {
                exportLayers.push(layer);
            }
        }
    }

    function safeFileName(name) {
        var cleaned = String(name).replace(/[\\\/:*?"<>|]/g, "_").replace(/^\s+|\s+$/g, "");
        return cleaned || "layer";
    }

    function uniqueName(base, usedNames) {
        var key = base.toLowerCase();
        if (!usedNames[key]) {
            usedNames[key] = 1;
            return base;
        }
        usedNames[key]++;
        return base + "_" + usedNames[key];
    }

    function hideAll() {
        for (var i = 0; i < allLayers.length; i++) {
            try {
                allLayers[i].visible = false;
            } catch (e) {}
        }
    }

    function showLayerAndParents(layer) {
        var parents = parentMap[layerId(layer)] || [];
        for (var p = 0; p < parents.length; p++) {
            try {
                parents[p].visible = true;
            } catch (e1) {}
        }
        try {
            layer.visible = true;
        } catch (e2) {}
    }

    function exportJpg(layer, backgroundLayer, usedNames) {
        hideAll();
        showLayerAndParents(backgroundLayer);
        showLayerAndParents(layer);

        var fileBase = uniqueName(safeFileName(layer.name), usedNames);
        var outFile = new File(outputFolder.fsName + "/" + fileBase + ".jpg");

        var jpgOptions = new JPEGSaveOptions();
        jpgOptions.quality = 12;
        jpgOptions.embedColorProfile = true;
        jpgOptions.formatOptions = FormatOptions.STANDARDBASELINE;
        jpgOptions.matte = MatteType.NONE;

        doc.saveAs(outFile, jpgOptions, true, Extension.LOWERCASE);
    }

    collect(doc, []);

    var backgroundLayer = null;
    try {
        backgroundLayer = doc.backgroundLayer;
    } catch (e1) {}

    if (!backgroundLayer) {
        for (var b = allLayers.length - 1; b >= 0; b--) {
            if (allLayers[b].typename !== "LayerSet" && isBackgroundLayer(allLayers[b])) {
                backgroundLayer = allLayers[b];
                break;
            }
        }
    }

    if (!backgroundLayer) {
        for (var fallback = allLayers.length - 1; fallback >= 0; fallback--) {
            if (allLayers[fallback].typename !== "LayerSet") {
                backgroundLayer = allLayers[fallback];
                break;
            }
        }
    }

    if (!backgroundLayer) {
        app.preferences.rulerUnits = originalRulerUnits;
        return fail("No normal layer was found to use as the background.");
    }

    var filteredExportLayers = [];
    for (var fl = 0; fl < exportLayers.length; fl++) {
        if (exportLayers[fl] !== backgroundLayer) {
            filteredExportLayers.push(exportLayers[fl]);
        }
    }
    exportLayers = filteredExportLayers;

    if (exportLayers.length === 0) {
        app.preferences.rulerUnits = originalRulerUnits;
        return fail("No exportable layers were found.");
    }

    var visibility = [];
    for (var v = 0; v < allLayers.length; v++) {
        visibility.push({
            layer: allLayers[v],
            visible: allLayers[v].visible
        });
    }

    var usedNames = {};
    var exportedCount = 0;
    var errorMessage = "";

    try {
        for (var e = 0; e < exportLayers.length; e++) {
            exportJpg(exportLayers[e], backgroundLayer, usedNames);
            exportedCount++;
        }
    } catch (err) {
        errorMessage = String(err);
    } finally {
        for (var r = visibility.length - 1; r >= 0; r--) {
            try {
                visibility[r].layer.visible = visibility[r].visible;
            } catch (restoreErr) {}
        }
        app.preferences.rulerUnits = originalRulerUnits;
    }

    if (errorMessage) {
        return fail("Export error: " + errorMessage + "\nExported: " + exportedCount + " file(s).\nFolder: " + outputFolder.fsName);
    }

    return "Done. Exported " + exportedCount + " JPG file(s).\nFolder: " + outputFolder.fsName;
})();
