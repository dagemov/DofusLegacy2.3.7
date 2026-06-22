#!/usr/bin/env node
/** F27 — wrap handler output into ResponseModel; enforce F26 view types */
import {
  buildSuccessResponse,
  buildErrorResponse,
  assertNoForbiddenExposure,
  isValidErrorCode,
} from './_gateway-lib.mjs';

function isContractError(value) {
  return value && value.error === true && typeof value.error_code === 'string';
}

export function projectGatewayResponse(request, handlerResult) {
  const { request_id: requestId } = request;

  if (isContractError(handlerResult)) {
    if (!isValidErrorCode(handlerResult.error_code)) {
      throw new Error(`Invalid error code from handler: ${handlerResult.error_code}`);
    }
    const response = buildErrorResponse(requestId, handlerResult);
    if (!assertNoForbiddenExposure(response)) {
      throw new Error('Forbidden exposure in error response');
    }
    return response;
  }

  const response = buildSuccessResponse(requestId, handlerResult);
  if (!assertNoForbiddenExposure(response)) {
    throw new Error('Forbidden exposure in success response');
  }
  return response;
}
