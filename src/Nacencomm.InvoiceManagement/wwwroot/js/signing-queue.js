/**
 * Signing Queue & Batch Manager
 * Sequential signing worker (concurrency = 1)
 */
const SigningQueue = (function () {
    let activeJob = null;
    let isCancelled = false;
    let isRunning = false;

    function generateGuid() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
            var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    }

    return {
        isProcessing: function () {
            return isRunning;
        },

        cancel: function () {
            if (isRunning) {
                isCancelled = true;
                console.log("Hủy tiến trình ký được yêu cầu.");
            }
        },

        /**
         * Start signing job for a list of selected invoice IDs
         * @param {Array<number>} invoiceIds 
         * @param {Object} callbacks { onProgress, onItemComplete, onFinished }
         */
        startJob: async function (invoiceIds, callbacks) {
            if (isRunning) {
                alert("Đang có một tiến trình ký đang chạy. Vui lòng đợi hoàn thành.");
                return;
            }

            if (!invoiceIds || invoiceIds.length === 0) {
                alert("Vui lòng chọn ít nhất 1 hóa đơn để ký.");
                return;
            }

            if (invoiceIds.length > 50) {
                alert("Một lượt ký chỉ cho phép chọn tối đa 50 hóa đơn.");
                return;
            }

            isCancelled = false;
            isRunning = true;

            try {
                // 1. Create Job in backend
                const createRes = await fetch('/api/Signing/create-job', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ invoiceIds: invoiceIds })
                });

                if (!createRes.ok) {
                    const err = await createRes.json();
                    alert(err.message || "Lỗi khởi tạo Job ký số.");
                    isRunning = false;
                    return;
                }

                activeJob = await createRes.json();
                console.log("Job ký số được tạo:", activeJob);

                if (callbacks.onJobCreated) {
                    callbacks.onJobCreated(activeJob);
                }

                // Check WinForms signer status first
                let signerStatus = await WinFormsSigner.checkStatus();
                if (!signerStatus.isRunning) {
                    console.warn("WinForms signer local HTTP not detected. Prompting dev mock mode option...");
                    // Try auto fallback if mock mode toggled, or notify user
                    if (!WinFormsSigner.isDevMockEnabled()) {
                        let enableMock = confirm("Không kết nối được ứng dụng WinForms Chữ ký số tại 127.0.0.1:8765.\n\nBạn có muốn kích hoạt 'Chế độ Giả lập (Mock Mode)' để thử nghiệm toàn bộ giao diện & tiến trình ký 50 hóa đơn không?");
                        if (enableMock) {
                            WinFormsSigner.enableDevMock(true);
                            signerStatus = await WinFormsSigner.checkStatus();
                        } else {
                            isRunning = false;
                            if (callbacks.onFinished) callbacks.onFinished(activeJob, false, "SIGNER_NOT_RUNNING");
                            return;
                        }
                    }
                }

                // 2. Process Items sequentially (Concurrency = 1)
                for (let i = 0; i < activeJob.items.length; i++) {
                    if (isCancelled) {
                        console.log("Dừng worker do người dùng bấm Hủy.");
                        activeJob.status = "Cancelled";
                        break;
                    }

                    const item = activeJob.items[i];
                    item.status = "GettingXml";

                    if (callbacks.onItemStatusChange) {
                        callbacks.onItemStatusChange(item, activeJob);
                    }

                    // Step A: Fetch Raw XML from Web server
                    let rawXml = null;
                    try {
                        const xmlRes = await fetch(`/Invoice/GetXml?id=${item.invoiceId}`);
                        if (xmlRes.ok) {
                            rawXml = await xmlRes.text();
                        } else {
                            throw new Error("Không lấy được XML hóa đơn từ server.");
                        }
                    } catch (err) {
                        item.status = "Failed";
                        item.errorCode = "GET_XML_FAILED";
                        item.errorMessage = err.message;
                        await this.reportItemResult(activeJob.jobId, item.invoiceId, false, null, item.errorCode, item.errorMessage);
                        activeJob.failed++;
                        activeJob.completed++;
                        if (callbacks.onItemStatusChange) callbacks.onItemStatusChange(item, activeJob);
                        continue;
                    }

                    // Step B: Sign XML via local WinForms HTTP Signer
                    item.status = "Signing";
                    if (callbacks.onItemStatusChange) callbacks.onItemStatusChange(item, activeJob);

                    const requestId = generateGuid();
                    const signResult = await WinFormsSigner.signXml(requestId, item.invoiceId, rawXml);

                    // Step C: Submit Result to Backend
                    item.status = "Submitting";
                    if (callbacks.onItemStatusChange) callbacks.onItemStatusChange(item, activeJob);

                    const submitOk = await this.reportItemResult(
                        activeJob.jobId,
                        item.invoiceId,
                        signResult.success,
                        signResult.signedXml,
                        signResult.errorCode,
                        signResult.errorMessage
                    );

                    if (signResult.success && submitOk) {
                        item.status = "Success";
                        activeJob.success++;
                    } else {
                        item.status = "Failed";
                        item.errorCode = signResult.errorCode || "SUBMIT_FAILED";
                        item.errorMessage = signResult.errorMessage || "Lỗi cập nhật kết quả ký về hệ thống.";
                        activeJob.failed++;
                    }
                    activeJob.completed++;

                    if (callbacks.onItemStatusChange) {
                        callbacks.onItemStatusChange(item, activeJob);
                    }
                }

                activeJob.status = isCancelled ? "Cancelled" : "Completed";
                if (callbacks.onFinished) {
                    callbacks.onFinished(activeJob, !isCancelled);
                }

            } catch (err) {
                console.error("Critical error in Signing Queue:", err);
                alert("Lỗi ngoài dự kiến trong quá trình xử lý ký: " + err.message);
            } finally {
                isRunning = false;
            }
        },

        reportItemResult: async function (jobId, invoiceId, success, signedXml, errorCode, errorMessage) {
            try {
                const res = await fetch(`/api/Signing/job/${jobId}/update-item`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        invoiceId: invoiceId,
                        success: success,
                        signedXml: signedXml,
                        errorCode: errorCode,
                        errorMessage: errorMessage
                    })
                });
                return res.ok;
            } catch (err) {
                console.error("Error submitting item result:", err);
                return false;
            }
        }
    };
})();
