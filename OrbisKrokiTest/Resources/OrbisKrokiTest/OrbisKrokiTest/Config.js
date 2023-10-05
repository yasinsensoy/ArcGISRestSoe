dojo.provide("arcgis.soe.OrbisKrokiTest.OrbisKrokiTest.Config");

dojo.require("dijit.form.ValidationTextBox");
dojo.require("dijit.form.CheckBox");
dojo.require("dijit.form.Select");
dojo.require("dijit._Templated");

dojo.require("esri.discovery.dijit.services._CustomSoeConfigurationPane");

dojo.declare("arcgis.soe.OrbisKrokiTest.OrbisKrokiTest.Config", [esri.discovery.dijit.services._CustomSoeConfigurationPane, dijit._Templated], {

    templatePath: dojo.moduleUrl("arcgis.soe.OrbisKrokiTest.OrbisKrokiTest", "OrbisKrokiPropertiesPane.html"),
    widgetsInTemplate: true,
    typeName: "OrbisKrokiPropertiesPane",
    _capabilities: null,

    // some UI element references...
    _useDynamicTileCheckBox: null,
    _useProxyCheckBox: null,
    _proxyTextBox: null,
    _outputPathSelect: null,
    _krokiRootPathTextBox: null,

    _setProperties: function (extension) {
        this.inherited(arguments); // REQUIRED... we need this so capabilities are automatically handled
        this.set({
            useDynamicTile: extension.properties.useDynamicTile,
            useProxy: extension.properties.useProxy,
            proxy: extension.properties.proxy,
            outputPath: extension.properties.outputPath,
            krokiRootPath: extension.properties.krokiRootPath
        });
    },

    getProperties: function () {
        var myCustomSoeProps = {
            properties: {
                useDynamicTile: this.get("useDynamicTile"),
                useProxy: this.get("useProxy"),
                proxy: this.get("proxy"),
                outputPath: this.get("outputPath"),
                krokiRootPath: this.get("krokiRootPath")
            }
        };

        // REQUIRED!!! This will overlay all your properties (capabilities) onto the default parent function return value
        return dojo.mixin(this.inherited(arguments), myCustomSoeProps);
    },

    // setters and getters... 
    // I use getters and setters in this example, but you could easily access the content of these widgets in the getProperties()
    // and _setProperties() functions respectively.  If you have some advanced business logic or validation to do, the getters/setters
    // can do the job.
    _setUseDynamicTileAttr: function (useDynamicTile) {
        this._useDynamicTileCheckBox.set("checked", useDynamicTile === true || useDynamicTile === "true");
    },

    _setUseProxyAttr: function (useDynamicTile) {
        this._useProxyCheckBox.set("checked", useDynamicTile === true || useDynamicTile === "true");
    },

    _setProxyAttr: function (proxy) {
        this._proxyTextBox.set("value", proxy);
    },

    _setOutputPathAttr: function (outputPath) {
        this._outputPathSelect.set("value", outputPath);
    },

    _setKrokiRootPathAttr: function (krokiRootPath) {
        this._krokiRootPathTextBox.set("value", krokiRootPath);
    },

    _getUseDynamicTileAttr: function () {
        return this._useDynamicTileCheckBox.get("checked");
    },

    _getUseProxyAttr: function () {
        return this._useProxyCheckBox.get("checked");
    },

    _getProxyAttr: function () {
        return this._proxyTextBox.get("value");
    },

    _getOutputPathAttr: function () {
        return this._outputPathSelect.get("value");
    },

    _getKrokiRootPathAttr: function () {
        return this._krokiRootPathTextBox.get("value");
    }
});
