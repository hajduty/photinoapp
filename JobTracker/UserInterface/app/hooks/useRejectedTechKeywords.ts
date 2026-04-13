import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { sendPhotinoRequest } from "../utils/photino";
import { notifications } from "@mantine/notifications";
import { GetRejectedTechKeywordsResponse } from "../types/settings/get-rejected-tech-keywords-response";
import { RemoveRejectedTechKeywordRequest, RemoveRejectedTechKeywordResponse } from "../types/settings/remove-rejected-tech-keyword-request";

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
        notifications.show({
          title: "Success",
          message: "Rejected keyword removed successfully.",
          color: "green",
        });
        queryClient.invalidateQueries({ queryKey: ["rejectedTechKeywords"] });
      } else {
        notifications.show({
          title: "Error",
          message: "Failed to remove rejected keyword.",
          color: "red",
        });
      }
    },
    onError: (err) => {
      notifications.show({
        title: "Error",
        message: `Failed to remove rejected keyword: ${err.message}`,
        color: "red",
      });
    },
  });

  return {
    rejectedKeywords: data?.RejectedKeywords || [],
    isLoadingRejectedKeywords: isLoading,
    rejectedKeywordsError: error,
    removeRejectedKeyword: (id: number) => removeKeywordMutation.mutate({ id }),
  };
}