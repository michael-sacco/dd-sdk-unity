// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2023-Present Datadog, Inc.

import Foundation
import DatadogCore
import DatadogRUM
import DatadogInternal

@_cdecl("DatadogRum_StartView")
func DatadogRum_StartView(key: CString?, name: CString?, attributes: CString?) {
    guard let core = DatadogUnityCore.shared,
          let key = decodeCString(cString: key) else {
        return
    }

    let name = decodeCString(cString: name)
    let decodedAttributes = decodeJsonAttributes(fromCString: attributes)

    RUMMonitor.shared(in: core).startView(key: key, name: name, attributes: decodedAttributes)
}

@_cdecl("DatadogRum_StopView")
func DatadogRum_StopView(key: CString?, attributes: CString?) {
    guard let core = DatadogUnityCore.shared,
          let key = decodeCString(cString: key) else {
        return
    }

    let decodedAttributes = decodeJsonAttributes(fromCString: attributes)

    RUMMonitor.shared(in: core).stopView(key: key, attributes: decodedAttributes)
}

@_cdecl("DatadogRum_AddAction")
func DatadogRum_AddAction(type: CString?, name: CString?, attributes: CString?) {
    guard let core = DatadogUnityCore.shared,
          let type = decodeUserActionType(fromCStirng: type),
          let name = decodeCString(cString: name) else {
        return
    }

    let decodedAttributes = decodeJsonAttributes(fromCString: attributes)

    RUMMonitor.shared(in: core).addAction(type: type, name: name, attributes: decodedAttributes)
}

@_cdecl("DatadogRum_StartAction")
func DatadogRum_StartAction(type: CString?, name: CString?, attributes: CString?) {
    guard let core = DatadogUnityCore.shared,
          let type = decodeUserActionType(fromCStirng: type),
          let name = decodeCString(cString: name) else {
        return
    }

    let decodedAttributes = decodeJsonAttributes(fromCString: attributes)

    RUMMonitor.shared(in: core).startAction(type: type, name: name, attributes: decodedAttributes)
}

@_cdecl("DatadogRum_StopAction")
func DatadogRum_StopAction(type: CString?, name: CString?, attributes: CString?) {
    guard let core = DatadogUnityCore.shared,
          let type = decodeUserActionType(fromCStirng: type),
          let name = decodeCString(cString: name) else {
        return
    }

    let decodedAttributes = decodeJsonAttributes(fromCString: attributes)

    RUMMonitor.shared(in: core).stopAction(type: type, name: name, attributes: decodedAttributes)
}

@_cdecl("DatadogRum_AddError")
func DatadogRum_AddError(message: CString?, source: CString?, type: CString?, stack: CString?, attributes: CString?) {
    guard let core = DatadogUnityCore.shared,
          let message = decodeCString(cString: message),
          let source = decodeErrorSource(fromCString: source) else {
              return
    }

    let type = decodeCString(cString: type)
    let stack = decodeCString(cString: stack)
    let decodedAttributes = decodeJsonAttributes(fromCString: attributes)

    RUMMonitor.shared(in: core).addError(message: message, type: type, stack: stack, source: source, attributes: decodedAttributes)
}

@_cdecl("DatadogRum_StartResource")
func DatadogRum_StartResource(key: CString?, httpMethod: CString?, url: CString, attributes: CString?) {
    guard let core = DatadogUnityCore.shared,
          let key = decodeCString(cString: key),
          let httpMethod = decodeHttpMethod(fromCString: httpMethod),
          let url = decodeCString(cString: url) else {
        return
    }

    let decodedAttributes = decodeJsonAttributes(fromCString: attributes)

    RUMMonitor.shared(in: core).startResource(resourceKey: key, httpMethod: httpMethod, urlString: url, attributes: decodedAttributes)
}

@_cdecl("DatadogRum_StopResource")
func DatadogRum_StopResource(key: CString?, resourceType: CString?, statusCode: Int,
                                    size: Int64, attributes: CString?) {
    guard let core = DatadogUnityCore.shared,
          let key = decodeCString(cString: key),
          let resourceType = decodeResourceType(fromCString: resourceType) else {
        return
    }

    let decodedAttributes = decodeJsonAttributes(fromCString: attributes)

    // Using -1 as a special value to mean nil, as passing optional ints in C# is difficult.
    let statusCode = statusCode == -1 ? nil : statusCode
    let size = size == -1 ? nil : size;

    RUMMonitor.shared(in: core).stopResource(resourceKey: key, statusCode: statusCode, kind: resourceType, size: size, attributes: decodedAttributes)
}

@_cdecl("DatadogRum_StopResourceWithError")
func DatadogRum_StopResourceWithError(key: CString?, errorType: CString?, errorMessage: CString?, attributes: CString?) {
    guard let core = DatadogUnityCore.shared,
          let key = decodeCString(cString: key),
          let errorMessage = decodeCString(cString: errorMessage) else {
        return
    }

    let errorType = decodeCString(cString: errorType)

    let decodedAttributes = decodeJsonAttributes(fromCString: attributes)

    RUMMonitor.shared(in: core).stopResourceWithError(resourceKey: key, message: errorMessage, type: errorType, response: nil, attributes: decodedAttributes)
}

@_cdecl("DatadogRum_AddAttribute")
func DatadogRum_AddAttribute(key: CString?, value: CString?) {
    guard let core = DatadogUnityCore.shared,
          let key = decodeCString(cString: key) else {
        return
    }

    let value = decodeJsonAttributes(fromCString: value)
    if let attrValue = value["value"] {
        RUMMonitor.shared(in: core).addAttribute(forKey: key, value: attrValue)
    }
}

@_cdecl("DatadogRum_RemoveAttribute")
func DatadogRum_RemoveAttribute(key: CString?) {
    guard let core = DatadogUnityCore.shared,
          let key = decodeCString(cString: key) else {
      return

  }

    RUMMonitor.shared(in: core).removeAttribute(forKey: key)
}

@_cdecl("DatadogRum_AddFeatureFlagEvaluation")
func DatadogRum_AddFeatureFlagEvaluation(key: CString?, value: CString?) {
    guard let core = DatadogUnityCore.shared,
          let key = decodeCString(cString: key),
          let value = decodeCString(cString: value) else {
        return
    }

    RUMMonitor.shared(in: core).addFeatureFlagEvaluation(name: key, value: value)
}

@_cdecl("DatadogRum_StopSession")
func DatadogRum_StopSession() {
    guard let core = DatadogUnityCore.shared else {
        return
    }

    RUMMonitor.shared(in: core).stopSession()
}

func decodeUserActionType(fromCStirng cStirng: CString?) -> RUMActionType? {
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
