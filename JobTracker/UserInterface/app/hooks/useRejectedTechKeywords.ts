import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { sendPhotinoRequest } from "../utils/photino";
import { notifications } from "@mantine/notifications";
import { GetRejectedTechKeywordsResponse } from "../types/settings/get-rejected-tech-keywords-response";
import { RemoveRejectedTechKeywordRequest, RemoveRejectedTechKeywordResponse } from "../types/settings/remove-rejected-tech-keyword-request";
import { KeywordScope } from "../types/settings/keyword-rule";

interface UpdateScopeRequest { TagId: number; Scope: KeywordScope }
interface UpdateScopeResponse { Success: boolean }

export function useRejectedTechKeywords() {
  const queryClient = useQueryClient();

  const { data, isLoading, error } = useQuery<GetRejectedTechKeywordsResponse>({
    queryKey: ["rejectedTechKeywords"],
    queryFn: () => sendPhotinoRequest("settings.getRejectedTechKeywords", {}),
  });

  const removeKeywordMutation = useMutation<RemoveRejectedTechKeywordResponse, Error, RemoveRejectedTechKeywordRequest>({
    mutationFn: (request) => sendPhotinoRequest("settings.removeRejectedTechKeyword", request),
    onSuccess: (response) => {
      if (response.Success) {
        queryClient.invalidateQueries({ queryKey: ["rejectedTechKeywords"] });
      } else {
        notifications.show({ title: "Error", message: "Failed to remove rejected keyword.", color: "red" });
      }
    },
    onError: (err) => {
      notifications.show({ title: "Error", message: `Failed to remove rejected keyword: ${err.message}`, color: "red" });
    },
  });

  const updateScopeMutation = useMutation<UpdateScopeResponse, Error, UpdateScopeRequest>({
    mutationFn: (request) => sendPhotinoRequest("settings.updateRejectedTagScope", request),
    onSuccess: (response) => {
      if (response.Success) {
        queryClient.invalidateQueries({ queryKey: ["rejectedTechKeywords"] });
      } else {
        notifications.show({ title: "Error", message: "Failed to update scope.", color: "red" });
      }
    },
    onError: (err) => {
      notifications.show({ title: "Error", message: `Failed to update scope: ${err.message}`, color: "red" });
    },
  });

  return {
    rejectedKeywords: data?.RejectedKeywords ?? [],
    isLoadingRejectedKeywords: isLoading,
    rejectedKeywordsError: error,
    removeRejectedKeyword: (id: number) => removeKeywordMutation.mutate({ id }),
    updateRejectedTagScope: (tagId: number, scope: KeywordScope) => updateScopeMutation.mutate({ TagId: tagId, Scope: scope }),
  };
}
