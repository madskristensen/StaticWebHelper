StaticWebHelper
===============

[![Build status](https://ci.appveyor.com/api/projects/status/pjx1c7v1r4rn1h7u)](https://ci.appveyor.com/project/madskristensen/staticwebhelper)

This NuGet package helps with static .html files. Here's what it does:

1. Minifies HTML including embedded script and style blocks
2. FingerPrints references to images, script and css files
3. Makes it easy to serve static resources from CDNs or cookieless domains
4. Handles Conditional GET requests (304's)
5. No code required. It works automatically after installing the NuGet Package.

It works by registering an HTTP Handler that takes over .html files. After
doing the transformations, it output caches the response until the .html file
itself or one of the referenced resources are updated on disk.

Install the [NuGet Package](http://www.nuget.org/packages/StaticWebHelper/)

### 1. Minifying HTML

To minify the HTML, StaticWebHeper uses `WebMarkupMin.Core` for the best and
safest minification. You can enable the minification through an appSetting:

```xml
<add key="minify" value="true" />
```

### 2. FingerPrinting

FingerPrinting is the process of appending a file version to any referenced
resources. This is important in order to do cache busting, so the resources
can have far-future experication dates.

Take this JavaScript reference:

```html
<script src="script/menu.js"></script>
```

The browser will automatically cache `scripts/menu.js`, so that when you
update the file, the browser will serve the old version from its cache.

To fix that, we need FingerPrinting. With StaticWebHelper the above `script`
tag will be rendered like this:

```html
<script src="script/menu.111559900000122255.js"></script>
```

The numbers are the `DateTime.Ticks` of when the file was last changed.

A URL rewrite is automatically being added to the `web.config` in order
to handle the new URL.

### 2. CDN or cookieless domain

To serve static files such as images, JavaScript and CSS files from a 
CDN or a cookieless domain is good for the performance of any website.

Typically, a sub domain is used to serve static files and with 
StaticWebHelper this is now easy to use. Instead of serving our 
JavaScript file from `/scripts/menu.js` we can now easily update the
reference to point to the sub domain/CDN.

```xml
<add key="cdnPath" value="http://static.mysite.com" />
```

The rendered `script` tag will then look like this:

```html
<script src="http://static.mysite.com/script/menu.111559900000122255.js"></script>
```
