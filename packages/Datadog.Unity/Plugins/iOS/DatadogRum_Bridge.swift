// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2023-Present Datadog, Inc.

import Foundation
import Datadog

@_cdecl("DatadogRum_StartView")
func DatadogRum_StartView(key: CString?, name: CString?, attributes: CString?) {
    if let key = decodeCString(cString: key) {

        let name = decodeCString(cString: name)
        let decodedAttributes = decodeJsonAttributes(fromCString: attributes)

        Global.rum.startView(key: key, name: name, attributes: decodedAttributes)
    }
}

@_cdecl("DatadogRum_StopView")
func DatadogRum_StopView(key: CString?, attributes: CString?) {
    if let key = decodeCString(cString: key) {

        let decodedAttributes = decodeJsonAttributes(fromCString: attributes)

        Global.rum.stopView(key: key, attributes: decodedAttributes)
    }
}

@_cdecl("DatadogRum_AddUserAction")
func DatadogRum_AddUserAction(type: CString?, name: CString?, attributes: CString?) {
    if let type = decodeUserActionType(fromCStirng: type),
       let name = decodeCString(cString: name) {

        let decodedAttributes = decodeJsonAttributes(fromCString: attributes)

        Global.rum.addUserAction(type: type, name: name, attributes: decodedAttributes)
    }
}

@_cdecl("DatadogRum_StartUserAction")
func DatadogRum_StartUserAction(type: CString?, name: CString?, attributes: CString?) {
    if let type = decodeUserActionType(fromCStirng: type),
       let name = decodeCString(cString: name) {

        let decodedAttributes = decodeJsonAttributes(fromCString: attributes)

        Global.rum.startUserAction(type: type, name: name, attributes: decodedAttributes)
    }
}

@_cdecl("DatadogRum_StopUserAction")
func DatadogRum_StopUserAction(type: CString?, name: CString?, attributes: CString?) {
    if let type = decodeUserActionType(fromCStirng: type),
       let name = decodeCString(cString: name) {

        let decodedAttributes = decodeJsonAttributes(fromCString: attributes)

        Global.rum.stopUserAction(type: type, name: name, attributes: decodedAttributes)
    }
}

@_cdecl("DatadogRum_AddError")
func DatadogRum_AddError(message: CString?, source: CString?, type: CString?, stack: CString?, attributes: CString?) {
    if let message = decodeCString(cString: message),
       let source = decodeErrorSource(fromCString: source) {

        let type = decodeCString(cString: type)
        let stack = decodeCString(cString: stack)
        let decodedAttributes = decodeJsonAttributes(fromCString: attributes)

        Global.rum.addError(message: message, type: type, source: source, stack: stack, attributes: decodedAttributes)
    }
}

@_cdecl("DatadogRum_StartResourceLoading")
func DatadogRum_StartResourceLoading(key: CString?, httpMethod: CString?, url: CString, attributes: CString?) {
    if let key = decodeCString(cString: key),
       let httpMethod = decodeHttpMethod(fromCString: httpMethod),
       let url = decodeCString(cString: url) {

        let decodedAttributes = decodeJsonAttributes(fromCString: attributes)

        Global.rum.startResourceLoading(resourceKey: key, httpMethod: httpMethod, urlString: url, attributes: decodedAttributes)
    }
}

@_cdecl("DatadogRum_StopResourceLoading")
func DatadogRum_StopResourceLoading(key: CString?, resourceType: CString?, statusCode: Int,
                                    size: Int64, attributes: CString?) {
    if let key = decodeCString(cString: key),
       let resourceType = decodeResourceType(fromCString: resourceType) {

        let decodedAttributes = decodeJsonAttributes(fromCString: attributes)

        // Using -1 as a special value to mean nil, as passing optional ints in C# is difficult.
        let statusCode = statusCode == -1 ? nil : statusCode
        let size = size == -1 ? nil : size;

        Global.rum.stopResourceLoading(resourceKey: key, statusCode: statusCode, kind: resourceType, size: size, attributes: decodedAttributes)
    }
}

@_cdecl("DatadogRum_StopResourceLoadingWithError")
func DatadogRum_StopResourceLoadingWithError(key: CString?, errorType: CString?, errorMessage: CString?, attributes: CString?) {
    if let key = decodeCString(cString: key),
       let errorMessage = decodeCString(cString: errorMessage) {

        let errorType = decodeCString(cString: errorType)

        let decodedAttributes = decodeJsonAttributes(fromCString: attributes)

        Global.rum.stopResourceLoadingWithError(resourceKey: key, errorMessage: errorMessage, type: errorType, attributes: decodedAttributes)
    }
}

@_cdecl("DatadogRum_AddAttribute")
func DatadogRum_AddAttribute(key: CString?, value: CString?) {
    if let key = decodeCString(cString: key) {
        let value = decodeJsonAttributes(fromCString: value)
        if let attrValue = value["value"] {
            Global.rum.addAttribute(forKey: key, value: attrValue)
        }
    }
}

@_cdecl("DatadogRum_RemoveAttribute")
func DatadogRum_RemoveAttribute(key: CString?) {
    if let key = decodeCString(cString: key) {
        Global.rum.removeAttribute(forKey: key)
    }
}

func decodeUserActionType(fromCStirng cStirng: CString?) -> RUMUserActionType? {
    guard let actionTypeString = decodeCString(cString: cStirng) else {
        return nil
    }

    switch actionTypeString {
    case "Tap": return .tap
    case "Scroll": return .scroll
    case "Swipe": return .swipe
    case "Custom": return .custom
    default:
        return nil
    }
}

func decodeErrorSource(fromCString cString: CString?) -> RUMErrorSource? {
    guard let errorSourceString = decodeCString(cString: cString) else {
        return nil
    }

    switch errorSourceString {
    case "Source": return .source
    case "Network": return .network
    case "WebView": return .webview
    case "Console": return .console
    case "Custom": return .custom
    default:
        return nil
    }
}

func decodeHttpMethod(fromCString cString: CString?) -> RUMMethod? {
    guard let httpMethodString = decodeCString(cString: cString) else {
        return nil
    }

    switch httpMethodString {
    case "Post": return .post
    case "Get": return .get
    case "Head": return .head
    case "Put": return .put
    case "Delete": return .delete
    case "Patch": return .patch
    default:
        return nil
    }
}

func decodeResourceType(fromCString cString: CString?) -> RUMResourceType? {
    guard let resourceTypeString = decodeCString(cString: cString) else {
        return nil
    }

    switch resourceTypeString {
    case "Document": return .document
    case "Image": return .image
    case "Xhr": return .xhr
    case "Beacon": return .beacon
    case "Css": return .css
    case "Fetch": return .fetch
    case "Font": return .font
    case "Js": return .js
    case "Media": return .media
    case "Other": return .other
    case "Native": return .native
    default:
        return nil
    }
}
