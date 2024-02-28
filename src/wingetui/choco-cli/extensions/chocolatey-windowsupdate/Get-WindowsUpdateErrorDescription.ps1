# https://support.microsoft.com/en-us/help/938205/windows-update-error-code-list
$WindowsUpdateErrorLookup = @{}
$WindowsUpdateErrorLookup[0xf0801] = @{ Name = 'CBS_S_BUSY'; Description = 'operation is still in progress' }
$WindowsUpdateErrorLookup[0xf0802] = @{ Name = 'CBS_S_ALREADY_EXISTS'; Description = 'source already exists, now copy not added' }
$WindowsUpdateErrorLookup[0xf0803] = @{ Name = 'CBS_S_STACK_SHUTDOWN_REQUIRED'; Description = 'servicing stack updated, aborting' }
$WindowsUpdateErrorLookup[0xf0800] = @{ Name = 'CBS_E_INTERNAL_ERROR'; Description = 'Reserved error (|); there is no message for this error' }
$WindowsUpdateErrorLookup[0xf0801] = @{ Name = 'CBS_E_NOT_INITIALIZED'; Description = 'session not initialized' }
$WindowsUpdateErrorLookup[0xf0802] = @{ Name = 'CBS_E_ALREADY_INITIALIZED'; Description = 'session already initialized' }
$WindowsUpdateErrorLookup[0xf0803] = @{ Name = 'CBS_E_INVALID_PARAMETER'; Description = 'invalid method argument' }
$WindowsUpdateErrorLookup[0xf0804] = @{ Name = 'CBS_E_OPEN_FAILED'; Description = 'the update could not be found or could not be opened' }
$WindowsUpdateErrorLookup[0xf0805] = @{ Name = 'CBS_E_INVALID_PACKAGE'; Description = 'the update package was not a valid CSI update' }
$WindowsUpdateErrorLookup[0xf0806] = @{ Name = 'CBS_E_PENDING'; Description = 'the operation could not be complete due to locked resources' }
$WindowsUpdateErrorLookup[0xf0807] = @{ Name = 'CBS_E_NOT_INSTALLABLE'; Description = 'the component referenced is not separately installable' }
$WindowsUpdateErrorLookup[0xf0808] = @{ Name = 'CBS_E_IMAGE_NOT_ACCESSIBLE'; Description = 'the image location specified could not be accessed' }
$WindowsUpdateErrorLookup[0xf0809] = @{ Name = 'CBS_E_ARRAY_ELEMENT_MISSING'; Description = 'attempt to get non-existent array element' }
$WindowsUpdateErrorLookup[0xf080A] = @{ Name = 'CBS_E_REESTABLISH_SESSION'; Description = 'session object updated, must recreate session' }
$WindowsUpdateErrorLookup[0xf080B] = @{ Name = 'CBS_E_PROPERTY_NOT_AVAILABLE'; Description = 'requested property is not supported' }
$WindowsUpdateErrorLookup[0xf080C] = @{ Name = 'CBS_E_UNKNOWN_UPDATE'; Description = 'named update not present in package' }
$WindowsUpdateErrorLookup[0xf080D] = @{ Name = 'CBS_E_MANIFEST_INVALID_ITEM'; Description = 'invalid attribute or element name encountered' }
$WindowsUpdateErrorLookup[0xf080E] = @{ Name = 'CBS_E_MANIFEST_VALIDATION_DUPLICATE_ATTRIBUTES'; Description = 'multiple attributes have the same name' }
$WindowsUpdateErrorLookup[0xf080F] = @{ Name = 'CBS_E_MANIFEST_VALIDATION_DUPLICATE_ELEMENT'; Description = 'multiple elements have the same name' }
$WindowsUpdateErrorLookup[0xf0810] = @{ Name = 'CBS_E_MANIFEST_VALIDATION_MISSING_REQUIRED_ATTRIBUTES'; Description = 'required attributes are missing' }
$WindowsUpdateErrorLookup[0xf0811] = @{ Name = 'CBS_E_MANIFEST_VALIDATION_MISSING_REQUIRED_ELEMENTS'; Description = 'required attributes are missing' }
$WindowsUpdateErrorLookup[0xf0812] = @{ Name = 'CBS_E_MANIFEST_VALIDATION_UPDATES_PARENT_MISSING'; Description = 'required attributes are missing' }
$WindowsUpdateErrorLookup[0xf0813] = @{ Name = 'CBS_E_INVALID_INSTALL_STATE'; Description = 'install state value not acceptable' }
$WindowsUpdateErrorLookup[0xf0814] = @{ Name = 'CBS_E_INVALID_CONFIG_VALUE'; Description = 'invalid setting configuration value' }
$WindowsUpdateErrorLookup[0xf0815] = @{ Name = 'CBS_E_INVALID_CARDINALITY'; Description = 'invalid cardinality' }
$WindowsUpdateErrorLookup[0xf0816] = @{ Name = 'CBS_E_DPX_JOB_STATE_SAVED'; Description = 'job state for DPX has been saved' }
$WindowsUpdateErrorLookup[0xf0817] = @{ Name = 'CBS_E_PACKAGE_DELETED'; Description = 'package was uninstalled and is no longer accessible' }
$WindowsUpdateErrorLookup[0xf0818] = @{ Name = 'CBS_E_IDENTITY_MISMATCH'; Description = 'container package points to a package manifest whose identity doesn''t match the identity specified' }
$WindowsUpdateErrorLookup[0xf0819] = @{ Name = 'CBS_E_DUPLICATE_UPDATENAME'; Description = 'update name is duplicated in package.' }
$WindowsUpdateErrorLookup[0xf081A] = @{ Name = 'CBS_E_INVALID_DRIVER_OPERATION_KEY'; Description = 'the driver operations key is corrupt or invalid' }
$WindowsUpdateErrorLookup[0xf081B] = @{ Name = 'CBS_E_UNEXPECTED_PROCESSOR_ARCHITECTURE'; Description = 'the processor architecture specified is not supported' }
$WindowsUpdateErrorLookup[0xf081C] = @{ Name = 'CBS_E_EXCESSIVE_EVALUATION'; Description = 'Watchlist: not able to reach steady state after too many attempts.' }
$WindowsUpdateErrorLookup[0xf081D] = @{ Name = 'CBS_E_CYCLE_EVALUATION'; Description = 'Watchlist: cycle appears when planning component intended state.' }
$WindowsUpdateErrorLookup[0xf081E] = @{ Name = 'CBS_E_NOT_APPLICABLE'; Description = 'the package is not applicable' }
$WindowsUpdateErrorLookup[0xf081F] = @{ Name = 'CBS_E_SOURCE_MISSING'; Description = 'source for package or file not found, ResolveSource() unsuccessful' }
$WindowsUpdateErrorLookup[0xf0820] = @{ Name = 'CBS_E_CANCEL'; Description = 'user cancel, IDCANCEL returned by ICbsUIHandler method except Error()' }
$WindowsUpdateErrorLookup[0xf0821] = @{ Name = 'CBS_E_ABORT'; Description = 'client abort, IDABORT returned by ICbsUIHandler method except Error()' }
$WindowsUpdateErrorLookup[0xf0822] = @{ Name = 'CBS_E_ILLEGAL_COMPONENT_UPDATE'; Description = 'Component update without specifying <updateComponent> in package manifest.' }
$WindowsUpdateErrorLookup[0xf0823] = @{ Name = 'CBS_E_NEW_SERVICING_STACK_REQUIRED'; Description = 'Package needs a newer version of the servicing stack.' }
$WindowsUpdateErrorLookup[0xf0824] = @{ Name = 'CBS_E_SOURCE_NOT_IN_LIST'; Description = 'Package source not in list.' }
$WindowsUpdateErrorLookup[0xf0825] = @{ Name = 'CBS_E_CANNOT_UNINSTALL'; Description = 'Package cannot be uninstalled.' }
$WindowsUpdateErrorLookup[0xf0826] = @{ Name = 'CBS_E_PENDING_VICTIM'; Description = 'Package failed to install because another pended package failed.' }
$WindowsUpdateErrorLookup[0xf0827] = @{ Name = 'CBS_E_STACK_SHUTDOWN_REQUIRED'; Description = 'servicing stack updated, aborting' }
$WindowsUpdateErrorLookup[0xf0900] = @{ Name = 'CBS_E_XML_PARSER_FAILURE'; Description = 'unexpected internal XML parser error.' }
$WindowsUpdateErrorLookup[0xf0901] = @{ Name = 'CBS_E_MANIFEST_VALIDATION_MULTIPLE_UPDATE_COMPONENT_ON_SAME_FAMILY_NOT_ALLOWED'; Description = 'In a given package, only one <updateComponent> Is allowed for a component family. ' }
$WindowsUpdateErrorLookup[0x00240001] = @{ Name = 'WU_S_SERVICE_STOP'; Description = 'WindowsUpdate Windows Update Agent was stopped successfully' }
$WindowsUpdateErrorLookup[0x00240002] = @{ Name = 'WU_S_SELFUPDATE'; Description = 'Windows Update Agent updated itself' }
$WindowsUpdateErrorLookup[0x00240003] = @{ Name = 'WU_S_UPDATE_ERROR'; Description = 'Operation completed successfully but there were errors applying the updates' }
$WindowsUpdateErrorLookup[0x00240004] = @{ Name = 'WU_S_MARKED_FOR_DISCONNECT'; Description = 'A callback was marked to be disconnected later because the request to disconnect the operation came while a callback was executing' }
$WindowsUpdateErrorLookup[0x00240005] = @{ Name = 'WU_S_REBOOT_REQUIRED'; Description = 'The system must be restarted to complete installation of the update' }
$WindowsUpdateErrorLookup[0x00240006] = @{ Name = 'WU_S_ALREADY_INSTALLED'; Description = 'The update to be installed is already installed on the system' }
$WindowsUpdateErrorLookup[0x00240007] = @{ Name = 'WU_S_ALREADY_UNINSTALLED'; Description = 'The update to be removed is not installed on the system' }
$WindowsUpdateErrorLookup[0x00240008] = @{ Name = 'WU_S_ALREADY_DOWNLOADED'; Description = 'The update to be downloaded has already been downloaded ' }
$WindowsUpdateErrorLookup[0x80240001] = @{ Name = 'WU_E_NO_SERVICE'; Description = 'Windows Update Agent was unable to provide the service.' }
$WindowsUpdateErrorLookup[0x80240002] = @{ Name = 'WU_E_MAX_CAPACITY_REACHED'; Description = 'The maximum capacity of the service was exceeded.' }
$WindowsUpdateErrorLookup[0x80240003] = @{ Name = 'WU_E_UNKNOWN_ID'; Description = 'An ID cannot be found.' }
$WindowsUpdateErrorLookup[0x80240004] = @{ Name = 'WU_E_NOT_INITIALIZED'; Description = 'The object could not be initialized.' }
$WindowsUpdateErrorLookup[0x80240005] = @{ Name = 'WU_E_RANGEOVERLAP'; Description = 'The update handler requested a byte range overlapping a previously requested range.' }
$WindowsUpdateErrorLookup[0x80240006] = @{ Name = 'WU_E_TOOMANYRANGES'; Description = 'The requested number of byte ranges exceeds the maximum number (2^31 - 1).' }
$WindowsUpdateErrorLookup[0x80240007] = @{ Name = 'WU_E_INVALIDINDEX'; Description = 'The index to a collection was invalid.' }
$WindowsUpdateErrorLookup[0x80240008] = @{ Name = 'WU_E_ITEMNOTFOUND'; Description = 'The key for the item queried could not be found.' }
$WindowsUpdateErrorLookup[0x80240009] = @{ Name = 'WU_E_OPERATIONINPROGRESS'; Description = 'Another conflicting operation was in progress. Some operations such as installation cannot be performed twice simultaneously.' }
$WindowsUpdateErrorLookup[0x8024000A] = @{ Name = 'WU_E_COULDNOTCANCEL'; Description = 'Cancellation of the operation was not allowed.' }
$WindowsUpdateErrorLookup[0x8024000B] = @{ Name = 'WU_E_CALL_CANCELLED'; Description = 'Operation was cancelled.' }
$WindowsUpdateErrorLookup[0x8024000C] = @{ Name = 'WU_E_NOOP'; Description = 'No operation was required.' }
$WindowsUpdateErrorLookup[0x8024000D] = @{ Name = 'WU_E_XML_MISSINGDATA'; Description = 'Windows Update Agent could not find required information in the update''s XML data.' }
$WindowsUpdateErrorLookup[0x8024000E] = @{ Name = 'WU_E_XML_INVALID'; Description = 'Windows Update Agent found invalid information in the update''s XML data.' }
$WindowsUpdateErrorLookup[0x8024000F] = @{ Name = 'WU_E_CYCLE_DETECTED'; Description = 'Circular update relationships were detected in the metadata.' }
$WindowsUpdateErrorLookup[0x80240010] = @{ Name = 'WU_E_TOO_DEEP_RELATION'; Description = 'Update relationships too deep to evaluate were evaluated.' }
$WindowsUpdateErrorLookup[0x80240011] = @{ Name = 'WU_E_INVALID_RELATIONSHIP'; Description = 'An invalid update relationship was detected.' }
$WindowsUpdateErrorLookup[0x80240012] = @{ Name = 'WU_E_REG_VALUE_INVALID'; Description = 'An invalid registry value was read.' }
$WindowsUpdateErrorLookup[0x80240013] = @{ Name = 'WU_E_DUPLICATE_ITEM'; Description = 'Operation tried to add a duplicate item to a list.' }
$WindowsUpdateErrorLookup[0x80240016] = @{ Name = 'WU_E_INSTALL_NOT_ALLOWED'; Description = 'Operation tried to install while another installation was in progress or the system was pending a mandatory restart.' }
$WindowsUpdateErrorLookup[0x80240017] = @{ Name = 'WU_E_NOT_APPLICABLE'; Description = 'Operation was not performed because there are no applicable updates.' }
$WindowsUpdateErrorLookup[0x80240018] = @{ Name = 'WU_E_NO_USERTOKEN'; Description = 'Operation failed because a required user token is missing.' }
$WindowsUpdateErrorLookup[0x80240019] = @{ Name = 'WU_E_EXCLUSIVE_INSTALL_CONFLICT'; Description = 'An exclusive update cannot be installed with other updates at the same time.' }
$WindowsUpdateErrorLookup[0x8024001A] = @{ Name = 'WU_E_POLICY_NOT_SET'; Description = 'A policy value was not set.' }
$WindowsUpdateErrorLookup[0x8024001B] = @{ Name = 'WU_E_SELFUPDATE_IN_PROGRESS'; Description = 'The operation could not be performed because the Windows Update Agent is self-updating.' }
$WindowsUpdateErrorLookup[0x8024001D] = @{ Name = 'WU_E_INVALID_UPDATE'; Description = 'An update contains invalid metadata.' }
$WindowsUpdateErrorLookup[0x8024001E] = @{ Name = 'WU_E_SERVICE_STOP'; Description = 'Operation did not complete because the service or system was being shut down.' }
$WindowsUpdateErrorLookup[0x8024001F] = @{ Name = 'WU_E_NO_CONNECTION'; Description = 'Operation did not complete because the network connection was unavailable.' }
$WindowsUpdateErrorLookup[0x80240020] = @{ Name = 'WU_E_NO_INTERACTIVE_USER'; Description = 'Operation did not complete because there is no logged-on interactive user.' }
$WindowsUpdateErrorLookup[0x80240021] = @{ Name = 'WU_E_TIME_OUT'; Description = 'Operation did not complete because it timed out.' }
$WindowsUpdateErrorLookup[0x80240022] = @{ Name = 'WU_E_ALL_UPDATES_FAILED'; Description = 'Operation failed for all the updates.' }
$WindowsUpdateErrorLookup[0x80240023] = @{ Name = 'WU_E_EULAS_DECLINED'; Description = 'The license terms for all updates were declined.' }
$WindowsUpdateErrorLookup[0x80240024] = @{ Name = 'WU_E_NO_UPDATE'; Description = 'There are no updates.' }
$WindowsUpdateErrorLookup[0x80240025] = @{ Name = 'WU_E_USER_ACCESS_DISABLED'; Description = 'Group Policy settings prevented access to Windows Update.' }
$WindowsUpdateErrorLookup[0x80240026] = @{ Name = 'WU_E_INVALID_UPDATE_TYPE'; Description = 'The type of update is invalid.' }
$WindowsUpdateErrorLookup[0x80240027] = @{ Name = 'WU_E_URL_TOO_LONG'; Description = 'The URL exceeded the maximum length.' }
$WindowsUpdateErrorLookup[0x80240028] = @{ Name = 'WU_E_UNINSTALL_NOT_ALLOWED'; Description = 'The update could not be uninstalled because the request did not originate from a WSUS server.' }
$WindowsUpdateErrorLookup[0x80240029] = @{ Name = 'WU_E_INVALID_PRODUCT_LICENSE'; Description = 'Search may have missed some updates before there is an unlicensed application on the system.' }
$WindowsUpdateErrorLookup[0x8024002A] = @{ Name = 'WU_E_MISSING_HANDLER'; Description = 'A component required to detect applicable updates was missing.' }
$WindowsUpdateErrorLookup[0x8024002B] = @{ Name = 'WU_E_LEGACYSERVER'; Description = 'An operation did not complete because it requires a newer version of server.' }
$WindowsUpdateErrorLookup[0x8024002C] = @{ Name = 'WU_E_BIN_SOURCE_ABSENT'; Description = 'A delta-compressed update could not be installed because it required the source.' }
$WindowsUpdateErrorLookup[0x8024002D] = @{ Name = 'WU_E_SOURCE_ABSENT'; Description = 'A full-file update could not be installed because it required the source.' }
$WindowsUpdateErrorLookup[0x8024002E] = @{ Name = 'WU_E_WU_DISABLED'; Description = 'Access to an unmanaged server is not allowed.' }
$WindowsUpdateErrorLookup[0x8024002F] = @{ Name = 'WU_E_CALL_CANCELLED_BY_POLICY'; Description = 'Operation did not complete because the DisableWindowsUpdateAccess policy was set.' }
$WindowsUpdateErrorLookup[0x80240030] = @{ Name = 'WU_E_INVALID_PROXY_SERVER'; Description = 'The format of the proxy list was invalid.' }
$WindowsUpdateErrorLookup[0x80240031] = @{ Name = 'WU_E_INVALID_FILE'; Description = 'The file is in the wrong format.' }
$WindowsUpdateErrorLookup[0x80240032] = @{ Name = 'WU_E_INVALID_CRITERIA'; Description = 'The search criteria string was invalid.' }
$WindowsUpdateErrorLookup[0x80240033] = @{ Name = 'WU_E_EULA_UNAVAILABLE'; Description = 'License terms could not be downloaded.' }
$WindowsUpdateErrorLookup[0x80240034] = @{ Name = 'WU_E_DOWNLOAD_FAILED'; Description = 'Update failed to download.' }
$WindowsUpdateErrorLookup[0x80240035] = @{ Name = 'WU_E_UPDATE_NOT_PROCESSED'; Description = 'The update was not processed.' }
$WindowsUpdateErrorLookup[0x80240036] = @{ Name = 'WU_E_INVALID_OPERATION'; Description = 'The object''s current state did not allow the operation.' }
$WindowsUpdateErrorLookup[0x80240037] = @{ Name = 'WU_E_NOT_SUPPORTED'; Description = 'The functionality for the operation is not supported.' }
$WindowsUpdateErrorLookup[0x80240038] = @{ Name = 'WU_E_WINHTTP_INVALID_FILE'; Description = 'The downloaded file has an unexpected content type.' }
$WindowsUpdateErrorLookup[0x80240039] = @{ Name = 'WU_E_TOO_MANY_RESYNC'; Description = 'Agent is asked by server to resync too many times.' }
$WindowsUpdateErrorLookup[0x80240040] = @{ Name = 'WU_E_NO_SERVER_CORE_SUPPORT'; Description = 'WUA API method does not run on Server Core installation.' }
$WindowsUpdateErrorLookup[0x80240041] = @{ Name = 'WU_E_SYSPREP_IN_PROGRESS'; Description = 'Service is not available while sysprep is running.' }
$WindowsUpdateErrorLookup[0x80240042] = @{ Name = 'WU_E_UNKNOWN_SERVICE'; Description = 'The update service is no longer registered with AU.' }
$WindowsUpdateErrorLookup[0x80240FFF] = @{ Name = 'WU_E_UNEXPECTED'; Description = 'An operation failed due to reasons not covered by another error code.' }
$WindowsUpdateErrorLookup[0x80241001] = @{ Name = 'WU_E_MSI_WRONG_VERSION'; Description = 'Search may have missed some updates because the Windows Installer is less than version 3.1.' }
$WindowsUpdateErrorLookup[0x80241002] = @{ Name = 'WU_E_MSI_NOT_CONFIGURED'; Description = 'Search may have missed some updates because the Windows Installer is not configured.' }
$WindowsUpdateErrorLookup[0x80241003] = @{ Name = 'WU_E_MSP_DISABLED'; Description = 'Search may have missed some updates because policy has disabled Windows Installer patching.' }
$WindowsUpdateErrorLookup[0x80241004] = @{ Name = 'WU_E_MSI_WRONG_APP_CONTEXT'; Description = 'An update could not be applied because the application is installed per-user.' }
$WindowsUpdateErrorLookup[0x80241FFF] = @{ Name = 'WU_E_MSP_UNEXPECTED'; Description = 'Search may have missed some updates because there was a failure of the Windows Installer. ' }
$WindowsUpdateErrorLookup[0x80244000] = @{ Name = 'WU_E_PT_SOAPCLIENT_BASE'; Description = 'WU_E_PT_SOAPCLIENT_* error codes map to the SOAPCLIENT_ERROR enum of the ATL Server Library.' }
$WindowsUpdateErrorLookup[0x80244001] = @{ Name = 'WU_E_PT_SOAPCLIENT_INITIALIZE'; Description = 'Same as SOAPCLIENT_INITIALIZE_ERROR - initialization of the SOAP client failed, possibly because of an MSXML installation failure.' }
$WindowsUpdateErrorLookup[0x80244002] = @{ Name = 'WU_E_PT_SOAPCLIENT_OUTOFMEMORY'; Description = 'Same as SOAPCLIENT_OUTOFMEMORY - SOAP client failed because it ran out of memory.' }
$WindowsUpdateErrorLookup[0x80244003] = @{ Name = 'WU_E_PT_SOAPCLIENT_GENERATE'; Description = 'Same as SOAPCLIENT_GENERATE_ERROR - SOAP client failed to generate the request.' }
$WindowsUpdateErrorLookup[0x80244004] = @{ Name = 'WU_E_PT_SOAPCLIENT_CONNECT'; Description = 'Same as SOAPCLIENT_CONNECT_ERROR - SOAP client failed to connect to the server.' }
$WindowsUpdateErrorLookup[0x80244005] = @{ Name = 'WU_E_PT_SOAPCLIENT_SEND'; Description = 'Same as SOAPCLIENT_SEND_ERROR - SOAP client failed to send a message for reasons of WU_E_WINHTTP_* error codes.' }
$WindowsUpdateErrorLookup[0x80244006] = @{ Name = 'WU_E_PT_SOAPCLIENT_SERVER'; Description = 'Same as SOAPCLIENT_SERVER_ERROR - SOAP client failed because there was a server error.' }
$WindowsUpdateErrorLookup[0x80244007] = @{ Name = 'WU_E_PT_SOAPCLIENT_SOAPFAULT'; Description = 'Same as SOAPCLIENT_SOAPFAULT - SOAP client failed because there was a SOAP fault for reasons of WU_E_PT_SOAP_* error codes.' }
$WindowsUpdateErrorLookup[0x80244008] = @{ Name = 'WU_E_PT_SOAPCLIENT_PARSEFAULT'; Description = 'Same as SOAPCLIENT_PARSEFAULT_ERROR - SOAP client failed to parse a SOAP fault.' }
$WindowsUpdateErrorLookup[0x80244009] = @{ Name = 'WU_E_PT_SOAPCLIENT_READ'; Description = 'Same as SOAPCLIENT_READ_ERROR - SOAP client failed while reading the response from the server.' }
$WindowsUpdateErrorLookup[0x8024400A] = @{ Name = 'WU_E_PT_SOAPCLIENT_PARSE'; Description = 'Same as SOAPCLIENT_PARSE_ERROR - SOAP client failed to parse the response from the server. ' }
$WindowsUpdateErrorLookup[0x8024400B] = @{ Name = 'WU_E_PT_SOAP_VERSION'; Description = 'Same as SOAP_E_VERSION_MISMATCH - SOAP client found an unrecognizable namespace for the SOAP envelope.' }
$WindowsUpdateErrorLookup[0x8024400C] = @{ Name = 'WU_E_PT_SOAP_MUST_UNDERSTAND'; Description = 'Same as SOAP_E_MUST_UNDERSTAND - SOAP client was unable to understand a header.' }
$WindowsUpdateErrorLookup[0x8024400D] = @{ Name = 'WU_E_PT_SOAP_CLIENT'; Description = 'Same as SOAP_E_CLIENT - SOAP client found the message was malformed; fix before resending.' }
$WindowsUpdateErrorLookup[0x8024400E] = @{ Name = 'WU_E_PT_SOAP_SERVER'; Description = 'Same as SOAP_E_SERVER - The SOAP message could not be processed due to a server error; resend later.' }
$WindowsUpdateErrorLookup[0x8024400F] = @{ Name = 'WU_E_PT_WMI_ERROR'; Description = 'There was an unspecified Windows Management Instrumentation (WMI) error.' }
$WindowsUpdateErrorLookup[0x80244010] = @{ Name = 'WU_E_PT_EXCEEDED_MAX_SERVER_TRIPS'; Description = 'The number of round trips to the server exceeded the maximum limit.' }
$WindowsUpdateErrorLookup[0x80244011] = @{ Name = 'WU_E_PT_SUS_SERVER_NOT_SET'; Description = 'WUServer policy value is missing in the registry.' }
$WindowsUpdateErrorLookup[0x80244012] = @{ Name = 'WU_E_PT_DOUBLE_INITIALIZATION'; Description = 'Initialization failed because the object was already initialized.' }
$WindowsUpdateErrorLookup[0x80244013] = @{ Name = 'WU_E_PT_INVALID_COMPUTER_NAME'; Description = 'The computer name could not be determined.' }
$WindowsUpdateErrorLookup[0x80244014] = @{ Name = 'WU_E_PT_INVALID_COMPUTER_LSID'; Description = 'Cannot determine computer LSID.' }
$WindowsUpdateErrorLookup[0x80244015] = @{ Name = 'WU_E_PT_REFRESH_CACHE_REQUIRED'; Description = 'The reply from the server indicates that the server was changed or the cookie was invalid; refresh the state of the internal cache and retry.' }
$WindowsUpdateErrorLookup[0x80244016] = @{ Name = 'WU_E_PT_HTTP_STATUS_BAD_REQUEST'; Description = 'Same as HTTP status 400 - the server could not process the request due to invalid syntax.' }
$WindowsUpdateErrorLookup[0x80244017] = @{ Name = 'WU_E_PT_HTTP_STATUS_DENIED'; Description = 'Same as HTTP status 401 - the requested resource requires user authentication.' }
$WindowsUpdateErrorLookup[0x80244018] = @{ Name = 'WU_E_PT_HTTP_STATUS_FORBIDDEN'; Description = 'Same as HTTP status 403 - server understood the request, but declined to fulfill it.' }
$WindowsUpdateErrorLookup[0x80244019] = @{ Name = 'WU_E_PT_HTTP_STATUS_NOT_FOUND'; Description = 'Same as HTTP status 404 - the server cannot find the requested URI (Uniform Resource Identifier)' }
$WindowsUpdateErrorLookup[0x8024401A] = @{ Name = 'WU_E_PT_HTTP_STATUS_BAD_METHOD'; Description = 'Same as HTTP status 405 - the HTTP method is not allowed.' }
$WindowsUpdateErrorLookup[0x8024401B] = @{ Name = 'WU_E_PT_HTTP_STATUS_PROXY_AUTH_REQ'; Description = 'Same as HTTP status 407 - proxy authentication is required.' }
$WindowsUpdateErrorLookup[0x8024401C] = @{ Name = 'WU_E_PT_HTTP_STATUS_REQUEST_TIMEOUT'; Description = 'Same as HTTP status 408 - the server timed out waiting for the request.' }
$WindowsUpdateErrorLookup[0x8024401D] = @{ Name = 'WU_E_PT_HTTP_STATUS_CONFLICT'; Description = 'Same as HTTP status 409 - the request was not completed due to a conflict with the current state of the resource.' }
$WindowsUpdateErrorLookup[0x8024401E] = @{ Name = 'WU_E_PT_HTTP_STATUS_GONE'; Description = 'Same as HTTP status 410 - requested resource is no longer available at the server.' }
$WindowsUpdateErrorLookup[0x8024401F] = @{ Name = 'WU_E_PT_HTTP_STATUS_SERVER_ERROR'; Description = 'Same as HTTP status 500 - an error internal to the server prevented fulfilling the request.' }
$WindowsUpdateErrorLookup[0x80244020] = @{ Name = 'WU_E_PT_HTTP_STATUS_NOT_SUPPORTED'; Description = 'Same as HTTP status 500 - server does not support the functionality required to fulfill the request.' }
$WindowsUpdateErrorLookup[0x80244021] = @{ Name = 'WU_E_PT_HTTP_STATUS_BAD_GATEWAY'; Description = 'Same as HTTP status 502 - the server, while acting as a gateway or proxy, received an invalid response from the upstream server it accessed in attempting to fulfill the request.' }
$WindowsUpdateErrorLookup[0x80244022] = @{ Name = 'WU_E_PT_HTTP_STATUS_SERVICE_UNAVAIL'; Description = 'Same as HTTP status 503 - the service is temporarily overloaded.' }
$WindowsUpdateErrorLookup[0x80244023] = @{ Name = 'WU_E_PT_HTTP_STATUS_GATEWAY_TIMEOUT'; Description = 'Same as HTTP status 503 - the request was timed out waiting for a gateway.' }
$WindowsUpdateErrorLookup[0x80244024] = @{ Name = 'WU_E_PT_HTTP_STATUS_VERSION_NOT_SUP'; Description = 'Same as HTTP status 505 - the server does not support the HTTP protocol version used for the request.' }
$WindowsUpdateErrorLookup[0x80244025] = @{ Name = 'WU_E_PT_FILE_LOCATIONS_CHANGED'; Description = 'Operation failed due to a changed file location; refresh internal state and resend.' }
$WindowsUpdateErrorLookup[0x80244026] = @{ Name = 'WU_E_PT_REGISTRATION_NOT_SUPPORTED'; Description = 'Operation failed because Windows Update Agent does not support registration with a non-WSUS server.' }
$WindowsUpdateErrorLookup[0x80244027] = @{ Name = 'WU_E_PT_NO_AUTH_PLUGINS_REQUESTED'; Description = 'The server returned an empty authentication information list.' }
$WindowsUpdateErrorLookup[0x80244028] = @{ Name = 'WU_E_PT_NO_AUTH_COOKIES_CREATED'; Description = 'Windows Update Agent was unable to create any valid authentication cookies.' }
$WindowsUpdateErrorLookup[0x80244029] = @{ Name = 'WU_E_PT_INVALID_CONFIG_PROP'; Description = 'A configuration property value was wrong.' }
$WindowsUpdateErrorLookup[0x8024402A] = @{ Name = 'WU_E_PT_CONFIG_PROP_MISSING'; Description = 'A configuration property value was missing.' }
$WindowsUpdateErrorLookup[0x8024402B] = @{ Name = 'WU_E_PT_HTTP_STATUS_NOT_MAPPED'; Description = 'The HTTP request could not be completed and the reason did not correspond to any of the WU_E_PT_HTTP_* error codes.' }
$WindowsUpdateErrorLookup[0x8024402C] = @{ Name = 'WU_E_PT_WINHTTP_NAME_NOT_RESOLVED'; Description = 'Same as ERROR_WINHTTP_NAME_NOT_RESOLVED - the proxy server or target server name cannot be resolved.' }
$WindowsUpdateErrorLookup[0x8024502D] = @{ Name = 'WU_E_PT_SAME_REDIR_ID'; Description = 'Windows Update Agent failed to download a redirector cabinet file with a new redirectorId value from the server during the recovery.' }
$WindowsUpdateErrorLookup[0x8024502E] = @{ Name = 'WU_E_PT_NO_MANAGED_RECOVER'; Description = 'A redirector recovery action did not complete because the server is managed.' }
$WindowsUpdateErrorLookup[0x8024402F] = @{ Name = 'WU_E_PT_ECP_SUCCEEDED_WITH_ERRORS'; Description = 'External cab file processing completed with some errors.' }
$WindowsUpdateErrorLookup[0x80244030] = @{ Name = 'WU_E_PT_ECP_INIT_FAILED'; Description = 'The external cab processor initialization did not complete.' }
$WindowsUpdateErrorLookup[0x80244031] = @{ Name = 'WU_E_PT_ECP_INVALID_FILE_FORMAT'; Description = 'The format of a metadata file was invalid.' }
$WindowsUpdateErrorLookup[0x80244032] = @{ Name = 'WU_E_PT_ECP_INVALID_METADATA'; Description = 'External cab processor found invalid metadata.' }
$WindowsUpdateErrorLookup[0x80244033] = @{ Name = 'WU_E_PT_ECP_FAILURE_TO_EXTRACT_DIGEST'; Description = 'The file digest could not be extracted from an external cab file.' }
$WindowsUpdateErrorLookup[0x80244034] = @{ Name = 'WU_E_PT_ECP_FAILURE_TO_DECOMPRESS_CAB_FILE'; Description = 'An external cab file could not be decompressed.' }
$WindowsUpdateErrorLookup[0x80244035] = @{ Name = 'WU_E_PT_ECP_FILE_LOCATION_ERROR'; Description = 'External cab processor was unable to get file locations.' }
$WindowsUpdateErrorLookup[0x80244FFF] = @{ Name = 'WU_E_PT_UNEXPECTED'; Description = 'A communication error not covered by another WU_E_PT_* error code. ' }
$WindowsUpdateErrorLookup[0x80245001] = @{ Name = 'WU_E_REDIRECTOR_LOAD_XML'; Description = 'The redirector XML document could not be loaded into the DOM class.' }
$WindowsUpdateErrorLookup[0x80245002] = @{ Name = 'WU_E_REDIRECTOR_S_FALSE'; Description = 'The redirector XML document is missing some required information.' }
$WindowsUpdateErrorLookup[0x80245003] = @{ Name = 'WU_E_REDIRECTOR_ID_SMALLER'; Description = 'The redirectorId in the downloaded redirector cab is less than in the cached cab.' }
$WindowsUpdateErrorLookup[0x80245FFF] = @{ Name = 'WU_E_REDIRECTOR_UNEXPECTED'; Description = 'The redirector failed for reasons not covered by another WU_E_REDIRECTOR_* error code.' }
$WindowsUpdateErrorLookup[0x8024C001] = @{ Name = 'WU_E_DRV_PRUNED'; Description = 'A driver was skipped.' }
$WindowsUpdateErrorLookup[0x8024C002] = @{ Name = 'WU_E_DRV_NOPROP_OR_LEGACY'; Description = 'A property for the driver could not be found. It may not conform with required specifications.' }
$WindowsUpdateErrorLookup[0x8024C003] = @{ Name = 'WU_E_DRV_REG_MISMATCH'; Description = 'The registry type read for the driver does not match the expected type.' }
$WindowsUpdateErrorLookup[0x8024C004] = @{ Name = 'WU_E_DRV_NO_METADATA'; Description = 'The driver update is missing metadata.' }
$WindowsUpdateErrorLookup[0x8024C005] = @{ Name = 'WU_E_DRV_MISSING_ATTRIBUTE'; Description = 'The driver update is missing a required attribute.' }
$WindowsUpdateErrorLookup[0x8024C006] = @{ Name = 'WU_E_DRV_SYNC_FAILED'; Description = 'Driver synchronization failed.' }
$WindowsUpdateErrorLookup[0x8024C007] = @{ Name = 'WU_E_DRV_NO_PRINTER_CONTENT'; Description = 'Information required for the synchronization of applicable printers is missing.' }
$WindowsUpdateErrorLookup[0x8024CFFF] = @{ Name = 'WU_E_DRV_UNEXPECTED'; Description = 'A driver error not covered by another WU_E_DRV_* code. ' }
$WindowsUpdateErrorLookup[0x80248000] = @{ Name = 'WU_E_DS_SHUTDOWN'; Description = 'An operation failed because Windows Update Agent is shutting down.' }
$WindowsUpdateErrorLookup[0x80248001] = @{ Name = 'WU_E_DS_INUSE'; Description = 'An operation failed because the data store was in use.' }
$WindowsUpdateErrorLookup[0x80248002] = @{ Name = 'WU_E_DS_INVALID'; Description = 'The current and expected states of the data store do not match.' }
$WindowsUpdateErrorLookup[0x80248003] = @{ Name = 'WU_E_DS_TABLEMISSING'; Description = 'The data store is missing a table.' }
$WindowsUpdateErrorLookup[0x80248004] = @{ Name = 'WU_E_DS_TABLEINCORRECT'; Description = 'The data store contains a table with unexpected columns.' }
$WindowsUpdateErrorLookup[0x80248005] = @{ Name = 'WU_E_DS_INVALIDTABLENAME'; Description = 'A table could not be opened because the table is not in the data store.' }
$WindowsUpdateErrorLookup[0x80248006] = @{ Name = 'WU_E_DS_BADVERSION'; Description = 'The current and expected versions of the data store do not match.' }
$WindowsUpdateErrorLookup[0x80248007] = @{ Name = 'WU_E_DS_NODATA'; Description = 'The information requested is not in the data store.' }
$WindowsUpdateErrorLookup[0x80248008] = @{ Name = 'WU_E_DS_MISSINGDATA'; Description = 'The data store is missing required information or has a NULL in a table column that requires a non-null value.' }
$WindowsUpdateErrorLookup[0x80248009] = @{ Name = 'WU_E_DS_MISSINGREF'; Description = 'The data store is missing required information or has a reference to missing license terms, file, localized property or linked row.' }
$WindowsUpdateErrorLookup[0x8024800A] = @{ Name = 'WU_E_DS_UNKNOWNHANDLER'; Description = 'The update was not processed because its update handler could not be recognized.' }
$WindowsUpdateErrorLookup[0x8024800B] = @{ Name = 'WU_E_DS_CANTDELETE'; Description = 'The update was not deleted because it is still referenced by one or more services.' }
$WindowsUpdateErrorLookup[0x8024800C] = @{ Name = 'WU_E_DS_LOCKTIMEOUTEXPIRED'; Description = 'The data store section could not be locked within the allotted time.' }
$WindowsUpdateErrorLookup[0x8024800D] = @{ Name = 'WU_E_DS_NOCATEGORIES'; Description = 'The category was not added because it contains no parent categories and is not a top-level category itself.' }
$WindowsUpdateErrorLookup[0x8024800E] = @{ Name = 'WU_E_DS_ROWEXISTS'; Description = 'The row was not added because an existing row has the same primary key.' }
$WindowsUpdateErrorLookup[0x8024800F] = @{ Name = 'WU_E_DS_STOREFILELOCKED'; Description = 'The data store could not be initialized because it was locked by another process.' }
$WindowsUpdateErrorLookup[0x80248010] = @{ Name = 'WU_E_DS_CANNOTREGISTER'; Description = 'The data store is not allowed to be registered with COM in the current process.' }
$WindowsUpdateErrorLookup[0x80248011] = @{ Name = 'WU_E_DS_UNABLETOSTART'; Description = 'Could not create a data store object in another process.' }
$WindowsUpdateErrorLookup[0x80248013] = @{ Name = 'WU_E_DS_DUPLICATEUPDATEID'; Description = 'The server sent the same update to the client with two different revision IDs.' }
$WindowsUpdateErrorLookup[0x80248014] = @{ Name = 'WU_E_DS_UNKNOWNSERVICE'; Description = 'An operation did not complete because the service is not in the data store.' }
$WindowsUpdateErrorLookup[0x80248015] = @{ Name = 'WU_E_DS_SERVICEEXPIRED'; Description = 'An operation did not complete because the registration of the service has expired.' }
$WindowsUpdateErrorLookup[0x80248016] = @{ Name = 'WU_E_DS_DECLINENOTALLOWED'; Description = 'A request to hide an update was declined because it is a mandatory update or because it was deployed with a deadline.' }
$WindowsUpdateErrorLookup[0x80248017] = @{ Name = 'WU_E_DS_TABLESESSIONMISMATCH'; Description = 'A table was not closed because it is not associated with the session.' }
$WindowsUpdateErrorLookup[0x80248018] = @{ Name = 'WU_E_DS_SESSIONLOCKMISMATCH'; Description = 'A table was not closed because it is not associated with the session.' }
$WindowsUpdateErrorLookup[0x80248019] = @{ Name = 'WU_E_DS_NEEDWINDOWSSERVICE'; Description = 'A request to remove the Windows Update service or to unregister it with Automatic Updates was declined because it is a built-in service and/or Automatic Updates cannot fall back to another service.' }
$WindowsUpdateErrorLookup[0x8024801A] = @{ Name = 'WU_E_DS_INVALIDOPERATION'; Description = 'A request was declined because the operation is not allowed.' }
$WindowsUpdateErrorLookup[0x8024801B] = @{ Name = 'WU_E_DS_SCHEMAMISMATCH'; Description = 'The schema of the current data store and the schema of a table in a backup XML document do not match.' }
$WindowsUpdateErrorLookup[0x8024801C] = @{ Name = 'WU_E_DS_RESETREQUIRED'; Description = 'The data store requires a session reset; release the session and retry with a new session.' }
$WindowsUpdateErrorLookup[0x8024801D] = @{ Name = 'WU_E_DS_IMPERSONATED'; Description = 'A data store operation did not complete because it was requested with an impersonated identity.' }
$WindowsUpdateErrorLookup[0x80248FFF] = @{ Name = 'WU_E_DS_UNEXPECTED'; Description = 'A data store error not covered by another WU_E_DS_* code. ' }
$WindowsUpdateErrorLookup[0x80249001] = @{ Name = 'WU_E_INVENTORY_PARSEFAILED'; Description = 'Parsing of the rule file failed.' }
$WindowsUpdateErrorLookup[0x80249002] = @{ Name = 'WU_E_INVENTORY_GET_INVENTORY_TYPE_FAILED'; Description = 'Failed to get the requested inventory type from the server.' }
$WindowsUpdateErrorLookup[0x80249003] = @{ Name = 'WU_E_INVENTORY_RESULT_UPLOAD_FAILED'; Description = 'Failed to upload inventory result to the server.' }
$WindowsUpdateErrorLookup[0x80249004] = @{ Name = 'WU_E_INVENTORY_UNEXPECTED'; Description = 'There was an inventory error not covered by another error code.' }
$WindowsUpdateErrorLookup[0x80249005] = @{ Name = 'WU_E_INVENTORY_WMI_ERROR'; Description = 'A WMI error occurred when enumerating the instances for a particular class.' }
$WindowsUpdateErrorLookup[0x8024A000] = @{ Name = 'WU_E_AU_NOSERVICE'; Description = 'Automatic Updates was unable to service incoming requests.' }
$WindowsUpdateErrorLookup[0x8024A002] = @{ Name = 'WU_E_AU_NONLEGACYSERVER'; Description = 'The old version of the Automatic Updates client has stopped because the WSUS server has been upgraded.' }
$WindowsUpdateErrorLookup[0x8024A003] = @{ Name = 'WU_E_AU_LEGACYCLIENTDISABLED'; Description = 'The old version of the Automatic Updates client was disabled.' }
$WindowsUpdateErrorLookup[0x8024A004] = @{ Name = 'WU_E_AU_PAUSED'; Description = 'Automatic Updates was unable to process incoming requests because it was paused.' }
$WindowsUpdateErrorLookup[0x8024A005] = @{ Name = 'WU_E_AU_NO_REGISTERED_SERVICE'; Description = 'No unmanaged service is registered with AU.' }
$WindowsUpdateErrorLookup[0x8024AFFF] = @{ Name = 'WU_E_AU_UNEXPECTED'; Description = 'An Automatic Updates error not covered by another WU_E_AU * code. ' }
$WindowsUpdateErrorLookup[0x80242000] = @{ Name = 'WU_E_UH_REMOTEUNAVAILABLE'; Description = 'A request for a remote update handler could not be completed because no remote process is available.' }
$WindowsUpdateErrorLookup[0x80242001] = @{ Name = 'WU_E_UH_LOCALONLY'; Description = 'A request for a remote update handler could not be completed because the handler is local only.' }
$WindowsUpdateErrorLookup[0x80242002] = @{ Name = 'WU_E_UH_UNKNOWNHANDLER'; Description = 'A request for an update handler could not be completed because the handler could not be recognized.' }
$WindowsUpdateErrorLookup[0x80242003] = @{ Name = 'WU_E_UH_REMOTEALREADYACTIVE'; Description = 'A remote update handler could not be created because one already exists.' }
$WindowsUpdateErrorLookup[0x80242004] = @{ Name = 'WU_E_UH_DOESNOTSUPPORTACTION'; Description = 'A request for the handler to install (uninstall) an update could not be completed because the update does not support install (uninstall).' }
$WindowsUpdateErrorLookup[0x80242005] = @{ Name = 'WU_E_UH_WRONGHANDLER'; Description = 'An operation did not complete because the wrong handler was specified.' }
$WindowsUpdateErrorLookup[0x80242006] = @{ Name = 'WU_E_UH_INVALIDMETADATA'; Description = 'A handler operation could not be completed because the update contains invalid metadata.' }
$WindowsUpdateErrorLookup[0x80242007] = @{ Name = 'WU_E_UH_INSTALLERHUNG'; Description = 'An operation could not be completed because the installer exceeded the time limit.' }
$WindowsUpdateErrorLookup[0x80242008] = @{ Name = 'WU_E_UH_OPERATIONCANCELLED'; Description = 'An operation being done by the update handler was cancelled.' }
$WindowsUpdateErrorLookup[0x80242009] = @{ Name = 'WU_E_UH_BADHANDLERXML'; Description = 'An operation could not be completed because the handler-specific metadata is invalid.' }
$WindowsUpdateErrorLookup[0x8024200A] = @{ Name = 'WU_E_UH_CANREQUIREINPUT'; Description = 'A request to the handler to install an update could not be completed because the update requires user input.' }
$WindowsUpdateErrorLookup[0x8024200B] = @{ Name = 'WU_E_UH_INSTALLERFAILURE'; Description = 'The installer failed to install (uninstall) one or more updates.' }
$WindowsUpdateErrorLookup[0x8024200C] = @{ Name = 'WU_E_UH_FALLBACKTOSELFCONTAINED'; Description = 'The update handler should download self-contained content rather than delta-compressed content for the update.' }
$WindowsUpdateErrorLookup[0x8024200D] = @{ Name = 'WU_E_UH_NEEDANOTHERDOWNLOAD'; Description = 'The update handler did not install the update because it needs to be downloaded again.' }
$WindowsUpdateErrorLookup[0x8024200E] = @{ Name = 'WU_E_UH_NOTIFYFAILURE'; Description = 'The update handler failed to send notification of the status of the install (uninstall) operation.' }
$WindowsUpdateErrorLookup[0x8024200F] = @{ Name = 'WU_E_UH_INCONSISTENT_FILE_NAMES'; Description = 'The file names contained in the update metadata and in the update package are inconsistent.' }
$WindowsUpdateErrorLookup[0x80242010] = @{ Name = 'WU_E_UH_FALLBACKERROR'; Description = 'The update handler failed to fall back to the self-contained content.' }
$WindowsUpdateErrorLookup[0x80242011] = @{ Name = 'WU_E_UH_TOOMANYDOWNLOADREQUESTS'; Description = 'The update handler has exceeded the maximum number of download requests.' }
$WindowsUpdateErrorLookup[0x80242012] = @{ Name = 'WU_E_UH_UNEXPECTEDCBSRESPONSE'; Description = 'The update handler has received an unexpected response from CBS.' }
$WindowsUpdateErrorLookup[0x80242013] = @{ Name = 'WU_E_UH_BADCBSPACKAGEID'; Description = 'The update metadata contains an invalid CBS package identifier.' }
$WindowsUpdateErrorLookup[0x80242014] = @{ Name = 'WU_E_UH_POSTREBOOTSTILLPENDING'; Description = 'The post-reboot operation for the update is still in progress.' }
$WindowsUpdateErrorLookup[0x80242015] = @{ Name = 'WU_E_UH_POSTREBOOTRESULTUNKNOWN'; Description = 'The result of the post-reboot operation for the update could not be determined.' }
$WindowsUpdateErrorLookup[0x80242016] = @{ Name = 'WU_E_UH_POSTREBOOTUNEXPECTEDSTATE'; Description = 'The state of the update after its post-reboot operation has completed is unexpected.' }
$WindowsUpdateErrorLookup[0x80242017] = @{ Name = 'WU_E_UH_NEW_SERVICING_STACK_REQUIRED'; Description = 'The OS servicing stack must be updated before this update is downloaded or installed.' }
$WindowsUpdateErrorLookup[0x80242FFF] = @{ Name = 'WU_E_UH_UNEXPECTED'; Description = 'An update handler error not covered by another WU_E_UH_* code. ' }
$WindowsUpdateErrorLookup[0x80246001] = @{ Name = 'WU_E_DM_URLNOTAVAILABLE'; Description = 'A download manager operation could not be completed because the requested file does not have a URL.' }
$WindowsUpdateErrorLookup[0x80246002] = @{ Name = 'WU_E_DM_INCORRECTFILEHASH'; Description = 'A download manager operation could not be completed because the file digest was not recognized.' }
$WindowsUpdateErrorLookup[0x80246003] = @{ Name = 'WU_E_DM_UNKNOWNALGORITHM'; Description = 'A download manager operation could not be completed because the file metadata requested an unrecognized hash algorithm.' }
$WindowsUpdateErrorLookup[0x80246004] = @{ Name = 'WU_E_DM_NEEDDOWNLOADREQUEST'; Description = 'An operation could not be completed because a download request is required from the download handler.' }
$WindowsUpdateErrorLookup[0x80246005] = @{ Name = 'WU_E_DM_NONETWORK'; Description = 'A download manager operation could not be completed because the network connection was unavailable.' }
$WindowsUpdateErrorLookup[0x80246006] = @{ Name = 'WU_E_DM_WRONGBITSVERSION'; Description = 'A download manager operation could not be completed because the version of Background Intelligent Transfer Service (BITS) is incompatible.' }
$WindowsUpdateErrorLookup[0x80246007] = @{ Name = 'WU_E_DM_NOTDOWNLOADED'; Description = 'The update has not been downloaded.' }
$WindowsUpdateErrorLookup[0x80246008] = @{ Name = 'WU_E_DM_FAILTOCONNECTTOBITS'; Description = 'A download manager operation failed because the download manager was unable to connect the Background Intelligent Transfer Service (BITS).' }
$WindowsUpdateErrorLookup[0x80246009] = @{ Name = 'WU_E_DM_BITSTRANSFERERROR'; Description = 'A download manager operation failed because there was an unspecified Background Intelligent Transfer Service (BITS) transfer error.' }
$WindowsUpdateErrorLookup[0x8024600A] = @{ Name = 'WU_E_DM_DOWNLOADLOCATIONCHANGED'; Description = 'A download must be restarted because the location of the source of the download has changed.' }
$WindowsUpdateErrorLookup[0x8024600B] = @{ Name = 'WU_E_DM_CONTENTCHANGED'; Description = 'A download must be restarted because the update content changed in a new revision.' }
$WindowsUpdateErrorLookup[0x80246FFF] = @{ Name = 'WU_E_DM_UNEXPECTED'; Description = 'There was a download manager error not covered by another WU_E_DM_* error code. ' }
$WindowsUpdateErrorLookup[0x8024D001] = @{ Name = 'WU_E_SETUP_INVALID_INFDATA'; Description = 'Windows Update Agent could not be updated because an INF file contains invalid information.' }
$WindowsUpdateErrorLookup[0x8024D002] = @{ Name = 'WU_E_SETUP_INVALID_IDENTDATA'; Description = 'Windows Update Agent could not be updated because the wuident.cab file contains invalid information.' }
$WindowsUpdateErrorLookup[0x8024D003] = @{ Name = 'WU_E_SETUP_ALREADY_INITIALIZED'; Description = 'Windows Update Agent could not be updated because of an internal error that caused setup initialization to be performed twice.' }
$WindowsUpdateErrorLookup[0x8024D004] = @{ Name = 'WU_E_SETUP_NOT_INITIALIZED'; Description = 'Windows Update Agent could not be updated because setup initialization never completed successfully.' }
$WindowsUpdateErrorLookup[0x8024D005] = @{ Name = 'WU_E_SETUP_SOURCE_VERSION_MISMATCH'; Description = 'Windows Update Agent could not be updated because the versions specified in the INF do not match the actual source file versions.' }
$WindowsUpdateErrorLookup[0x8024D006] = @{ Name = 'WU_E_SETUP_TARGET_VERSION_GREATER'; Description = 'Windows Update Agent could not be updated because a WUA file on the target system is newer than the corresponding source file.' }
$WindowsUpdateErrorLookup[0x8024D007] = @{ Name = 'WU_E_SETUP_REGISTRATION_FAILED'; Description = 'Windows Update Agent could not be updated because regsvr32.exe returned an error.' }
$WindowsUpdateErrorLookup[0x8024D008] = @{ Name = 'WU_E_SELFUPDATE_SKIP_ON_FAILURE'; Description = 'An update to the Windows Update Agent was skipped because previous attempts to update have failed.' }
$WindowsUpdateErrorLookup[0x8024D009] = @{ Name = 'WU_E_SETUP_SKIP_UPDATE'; Description = 'An update to the Windows Update Agent was skipped due to a directive in the wuident.cab file.' }
$WindowsUpdateErrorLookup[0x8024D00A] = @{ Name = 'WU_E_SETUP_UNSUPPORTED_CONFIGURATION'; Description = 'Windows Update Agent could not be updated because the current system configuration is not supported.' }
$WindowsUpdateErrorLookup[0x8024D00B] = @{ Name = 'WU_E_SETUP_BLOCKED_CONFIGURATION'; Description = 'Windows Update Agent could not be updated because the system is configured to block the update.' }
$WindowsUpdateErrorLookup[0x8024D00C] = @{ Name = 'WU_E_SETUP_REBOOT_TO_FIX'; Description = 'Windows Update Agent could not be updated because a restart of the system is required.' }
$WindowsUpdateErrorLookup[0x8024D00D] = @{ Name = 'WU_E_SETUP_ALREADYRUNNING'; Description = 'Windows Update Agent setup is already running.' }
$WindowsUpdateErrorLookup[0x8024D00E] = @{ Name = 'WU_E_SETUP_REBOOTREQUIRED'; Description = 'Windows Update Agent setup package requires a reboot to complete installation.' }
$WindowsUpdateErrorLookup[0x8024D00F] = @{ Name = 'WU_E_SETUP_HANDLER_EXEC_FAILURE'; Description = 'Windows Update Agent could not be updated because the setup handler failed during execution.' }
$WindowsUpdateErrorLookup[0x8024D010] = @{ Name = 'WU_E_SETUP_INVALID_REGISTRY_DATA'; Description = 'Windows Update Agent could not be updated because the registry contains invalid information.' }
$WindowsUpdateErrorLookup[0x8024D011] = @{ Name = 'WU_E_SELFUPDATE_REQUIRED'; Description = 'Windows Update Agent must be updated before search can continue.' }
$WindowsUpdateErrorLookup[0x8024D012] = @{ Name = 'WU_E_SELFUPDATE_REQUIRED_ADMIN'; Description = 'Windows Update Agent must be updated before search can continue. An administrator is required to perform the operation.' }
$WindowsUpdateErrorLookup[0x8024D013] = @{ Name = 'WU_E_SETUP_WRONG_SERVER_VERSION'; Description = 'Windows Update Agent could not be updated because the server does not contain update information for this version.' }
$WindowsUpdateErrorLookup[0x8024DFFF] = @{ Name = 'WU_E_SETUP_UNEXPECTED'; Description = 'Windows Update Agent could not be updated because of an error not covered by another WU_E_SETUP_* error code. ' }
$WindowsUpdateErrorLookup[0x8024E001] = @{ Name = 'WU_E_EE_UNKNOWN_EXPRESSION'; Description = 'An expression evaluator operation could not be completed because an expression was unrecognized.' }
$WindowsUpdateErrorLookup[0x8024E002] = @{ Name = 'WU_E_EE_INVALID_EXPRESSION'; Description = 'An expression evaluator operation could not be completed because an expression was invalid.' }
$WindowsUpdateErrorLookup[0x8024E003] = @{ Name = 'WU_E_EE_MISSING_METADATA'; Description = 'An expression evaluator operation could not be completed because an expression contains an incorrect number of metadata nodes.' }
$WindowsUpdateErrorLookup[0x8024E004] = @{ Name = 'WU_E_EE_INVALID_VERSION'; Description = 'An expression evaluator operation could not be completed because the version of the serialized expression data is invalid.' }
$WindowsUpdateErrorLookup[0x8024E005] = @{ Name = 'WU_E_EE_NOT_INITIALIZED'; Description = 'The expression evaluator could not be initialized.' }
$WindowsUpdateErrorLookup[0x8024E006] = @{ Name = 'WU_E_EE_INVALID_ATTRIBUTEDATA'; Description = 'An expression evaluator operation could not be completed because there was an invalid attribute.' }
$WindowsUpdateErrorLookup[0x8024E007] = @{ Name = 'WU_E_EE_CLUSTER_ERROR'; Description = 'An expression evaluator operation could not be completed because the cluster state of the computer could not be determined.' }
$WindowsUpdateErrorLookup[0x8024EFFF] = @{ Name = 'WU_E_EE_UNEXPECTED'; Description = 'There was an expression evaluator error not covered by another WU_E_EE_* error code.' }
$WindowsUpdateErrorLookup[0x80243001] = @{ Name = 'WU_E_INSTALLATION_RESULTS_UNKNOWN_VERSION'; Description = 'The results of download and installation could not be read from the registry due to an unrecognized data format version.' }
$WindowsUpdateErrorLookup[0x80243002] = @{ Name = 'WU_E_INSTALLATION_RESULTS_INVALID_DATA'; Description = 'The results of download and installation could not be read from the registry due to an invalid data format.' }
$WindowsUpdateErrorLookup[0x80243003] = @{ Name = 'WU_E_INSTALLATION_RESULTS_NOT_FOUND'; Description = 'The results of download and installation are not available; the operation may have failed to start.' }
$WindowsUpdateErrorLookup[0x80243004] = @{ Name = 'WU_E_TRAYICON_FAILURE'; Description = 'A failure occurred when trying to create an icon in the taskbar notification area.' }
$WindowsUpdateErrorLookup[0x80243FFD] = @{ Name = 'WU_E_NON_UI_MODE'; Description = 'Unable to show UI when in non-UI mode; WU client UI modules may not be installed.' }
$WindowsUpdateErrorLookup[0x80243FFE] = @{ Name = 'WU_E_WUCLTUI_UNSUPPORTED_VERSION'; Description = 'Unsupported version of WU client UI exported functions.' }
$WindowsUpdateErrorLookup[0x80243FFF] = @{ Name = 'WU_E_AUCLIENT_UNEXPECTED'; Description = 'There was a user interface error not covered by another WU_E_AUCLIENT_* error code. ' }
$WindowsUpdateErrorLookup[0x8024F001] = @{ Name = 'WU_E_REPORTER_EVENTCACHECORRUPT'; Description = 'The event cache file was defective.' }
$WindowsUpdateErrorLookup[0x8024F002] = @{ Name = 'WU_E_REPORTER_EVENTNAMESPACEPARSEFAILED'; Description = 'The XML in the event namespace descriptor could not be parsed.' }
$WindowsUpdateErrorLookup[0x8024F003] = @{ Name = 'WU_E_INVALID_EVENT'; Description = 'The XML in the event namespace descriptor could not be parsed.' }
$WindowsUpdateErrorLookup[0x8024F004] = @{ Name = 'WU_E_SERVER_BUSY'; Description = 'The server rejected an event because the server was too busy.' }
$WindowsUpdateErrorLookup[0x8024FFFF] = @{ Name = 'WU_E_REPORTER_UNEXPECTED'; Description = 'There was a reporter error not covered by another error code.' }
$WindowsUpdateErrorLookup[0x80247001] = @{ Name = 'WU_E_OL_INVALID_SCANFILE'; Description = 'An operation could not be completed because the scan package was invalid.' }
$WindowsUpdateErrorLookup[0x80247002] = @{ Name = 'WU_E_OL_NEWCLIENT_REQUIRED'; Description = 'An operation could not be completed because the scan package requires a greater version of the Windows Update Agent.' }
$WindowsUpdateErrorLookup[0x80247FFF] = @{ Name = 'WU_E_OL_UNEXPECTED'; Description = 'Search using the scan package failed. ' }

function Get-WindowsUpdateErrorDescription
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)] [int] $ErrorCode
    )
    End
    {
        if ($WindowsUpdateErrorLookup.ContainsKey($ErrorCode))
        {
            return $WindowsUpdateErrorLookup[$ErrorCode]
        }
        else
        {
            return $null
        }
    }
}
