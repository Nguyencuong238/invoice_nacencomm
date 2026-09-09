/**
 * Invoice Manager UI Logic
 * Max 50 selections, AJAX filtering, Tab switching, Modal updates
 */
document.addEventListener('DOMContentLoaded', function () {
    initInvoiceEvents();
    checkSignerConnection();
});

function checkSignerConnection() {
    WinFormsSigner.checkStatus().then(status => {
        const statusBadge = document.getElementById('signerStatusBadge');
        if (statusBadge) {
            if (status.isRunning) {
                statusBadge.className = 'badge bg-success';
                statusBadge.innerHTML = `<i class="bi bi-shield-check me-1"></i> WinForms Signer (v${status.version || '2.0'}) - Sẵn sàng`;
            } else {
                statusBadge.className = 'badge bg-warning text-dark';
                statusBadge.innerHTML = `<i class="bi bi-exclamation-triangle me-1"></i> WinForms Signer - Chưa kết nối (127.0.0.1:8765)`;
            }
        }
    });
}

function initInvoiceEvents() {
    const selectAllCheckbox = document.getElementById('selectAllInvoices');
    const rowCheckboxes = document.querySelectorAll('.invoice-checkbox');
    const selectedCountSpan = document.getElementById('selectedInvoiceCount');
    const btnBatchSign = document.getElementById('btnBatchSign');

    function updateSelectionState() {
        const checkedBoxes = document.querySelectorAll('.invoice-checkbox:checked');
        const count = checkedBoxes.length;

        if (selectedCountSpan) {
            selectedCountSpan.textContent = `${count} / 50`;
        }

        if (btnBatchSign) {
            if (count > 0 && count <= 50) {
                btnBatchSign.disabled = false;
                btnBatchSign.classList.remove('btn-secondary', 'btn-primary');
                btnBatchSign.classList.add('btn-teal-sign');
            } else {
                btnBatchSign.disabled = true;
                btnBatchSign.classList.remove('btn-teal-sign', 'btn-primary');
                btnBatchSign.classList.add('btn-secondary');
            }
        }

        // Update Select All Checkbox state
        if (selectAllCheckbox) {
            const enabledBoxes = document.querySelectorAll('.invoice-checkbox:not(:disabled)');
            if (enabledBoxes.length > 0 && checkedBoxes.length === Math.min(enabledBoxes.length, 50)) {
                selectAllCheckbox.checked = true;
                selectAllCheckbox.indeterminate = false;
            } else if (checkedBoxes.length > 0) {
                selectAllCheckbox.checked = false;
                selectAllCheckbox.indeterminate = true;
            } else {
                selectAllCheckbox.checked = false;
                selectAllCheckbox.indeterminate = false;
            }
        }
    }

    // Individual checkbox click event
    rowCheckboxes.forEach(cb => {
        cb.addEventListener('change', function () {
            const checkedBoxes = document.querySelectorAll('.invoice-checkbox:checked');
            if (checkedBoxes.length > 50) {
                this.checked = false;
                alert("Chú ý: Một lượt ký chỉ cho phép chọn tối đa 50 hóa đơn!");
            }
            updateSelectionState();
        });
    });

    // Select All event (limit to max 50)
    if (selectAllCheckbox) {
        selectAllCheckbox.addEventListener('change', function () {
            const enabledBoxes = Array.from(document.querySelectorAll('.invoice-checkbox:not(:disabled)'));
            const isChecking = this.checked;

            let checkedCount = 0;
            enabledBoxes.forEach(cb => {
                if (isChecking && checkedCount < 50) {
                    cb.checked = true;
                    checkedCount++;
                } else {
                    cb.checked = false;
                }
            });

            if (isChecking && enabledBoxes.length > 50) {
                alert("Chú ý: Danh sách có hơn 50 hóa đơn, hệ thống tự động chọn 50 hóa đơn đầu tiên (sắp xếp theo Ngày xuất ASC) theo đúng quy chuẩn kế hoạch!");
            }

            updateSelectionState();
        });
    }

    // Tab status click handler
    const tabLinks = document.querySelectorAll('.nav-tab-status');
    tabLinks.forEach(link => {
        link.addEventListener('click', function (e) {
            e.preventDefault();
            tabLinks.forEach(t => t.classList.remove('active', 'fw-bold', 'text-teal', 'border-bottom', 'border-3', 'border-teal'));
            this.classList.add('active', 'fw-bold', 'text-teal', 'border-bottom', 'border-3', 'border-teal');

            const tabValue = this.getAttribute('data-tab');
            const hiddenTab = document.getElementById('hiddenTabStatus');
            if (hiddenTab) hiddenTab.value = tabValue;

            const posSubTabsContainer = document.getElementById('posSubTabsContainer');
            if (posSubTabsContainer) {
                if (tabValue === 'POS') {
                    posSubTabsContainer.classList.remove('d-none');
                } else {
                    posSubTabsContainer.classList.add('d-none');
                }
            }

            filterInvoicesAjax();
        });
    });

    // Subtab click handler for POS tab
    const subtabBtns = document.querySelectorAll('.pos-subtab-btn');
    subtabBtns.forEach(btn => {
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            subtabBtns.forEach(b => {
                b.classList.remove('btn-primary', 'text-white');
                b.classList.add('btn-outline-primary', 'bg-white');
            });
            this.classList.remove('btn-outline-primary', 'bg-white');
            this.classList.add('btn-primary', 'text-white');

            const subtabVal = this.getAttribute('data-subtab');
            const hiddenSubTab = document.getElementById('hiddenSubTabStatus');
            if (hiddenSubTab) hiddenSubTab.value = subtabVal;
            filterInvoicesAjax();
        });
    });

    // Batch Sign Button click handler
    if (btnBatchSign) {
        btnBatchSign.addEventListener('click', function () {
            const checkedBoxes = document.querySelectorAll('.invoice-checkbox:checked');
            const selectedIds = Array.from(checkedBoxes).map(cb => parseInt(cb.value));

            if (selectedIds.length === 0) {
                alert("Vui lòng chọn ít nhất 1 hóa đơn để ký.");
                return;
            }

            if (selectedIds.length > 50) {
                alert("Tối đa 50 hóa đơn cho 1 đợt ký.");
                return;
            }

            openSigningModal(selectedIds);
        });
    }

    updateSelectionState();
}

/**
 * Filter table rows dynamically client-side based on inline column inputs
 */
function applyInlineColumnFilter() {
    const invNumVal = (document.getElementById('filterColInvNum')?.value || '').trim().toLowerCase();
    const buyerVal = (document.getElementById('filterColBuyer')?.value || '').trim().toLowerCase();
    const lookupVal = (document.getElementById('filterColLookup')?.value || '').trim().toLowerCase();
    const cqtVal = (document.getElementById('filterColCqt')?.value || '').trim().toLowerCase();

    const dataRows = document.querySelectorAll('.invoice-data-row');
    dataRows.forEach(row => {
        const rowInvNum = (row.getAttribute('data-invnum') || '').toLowerCase();
        const rowBuyer = (row.getAttribute('data-buyer') || '').toLowerCase();
        const rowLookup = (row.getAttribute('data-lookup') || '').toLowerCase();
        const rowCqt = (row.getAttribute('data-cqt') || '').toLowerCase();

        const matchInvNum = !invNumVal || rowInvNum.includes(invNumVal);
        const matchBuyer = !buyerVal || rowBuyer.includes(buyerVal);
        const matchLookup = !lookupVal || rowLookup.includes(lookupVal);
        const matchCqt = !cqtVal || rowCqt.includes(cqtVal);

        const subRow = row.nextElementSibling && row.nextElementSibling.classList.contains('invoice-sub-row') ? row.nextElementSibling : null;

        if (matchInvNum && matchBuyer && matchLookup && matchCqt) {
            row.style.display = '';
            if (subRow) subRow.style.display = '';
        } else {
            row.style.display = 'none';
            if (subRow) subRow.style.display = 'none';
        }
    });
}

function triggerSingleSign(invoiceId) {
    if (confirm(`Bạn có muốn khởi tạo tiến trình ký số cho hóa đơn #${invoiceId} không?`)) {
        openSigningModal([invoiceId]);
    }
}

function filterInvoicesAjax() {
    const form = document.getElementById('invoiceFilterForm');
    if (!form) return;

    const formData = new FormData(form);
    const searchParams = new URLSearchParams(formData);

    const tableContainer = document.getElementById('invoiceTableContainer');
    if (tableContainer) {
        tableContainer.style.opacity = '0.5';
    }

    fetch(`/Invoice/GetInvoicesPartial?${searchParams.toString()}`)
        .then(res => res.text())
        .then(html => {
            if (tableContainer) {
                tableContainer.innerHTML = html;
                tableContainer.style.opacity = '1';
                initInvoiceEvents();
            }
        })
        .catch(err => {
            console.error("Lỗi tải lại danh sách hóa đơn:", err);
            if (tableContainer) tableContainer.style.opacity = '1';
        });
}

/**
 * Trigger Excel Export using current filter parameters
 */
function exportInvoicesExcel() {
    const form = document.getElementById('invoiceFilterForm');
    let searchParams = '';
    if (form) {
        const formData = new FormData(form);
        searchParams = new URLSearchParams(formData).toString();
    }
    window.location.href = `/Invoice/ExportExcel?${searchParams}`;
}

/**
 * Open Signing Progress Modal and start queue
 */
function openSigningModal(selectedIds) {
    const modalElem = document.getElementById('signingProgressModal');
    if (!modalElem) return;

    let bsModal = bootstrap.Modal.getInstance(modalElem);
    if (!bsModal) {
        bsModal = new bootstrap.Modal(modalElem, { backdrop: 'static', keyboard: false });
    }

    // Reset Modal UI safely
    const totalCountElem = document.getElementById('modalTotalCount');
    const completedCountElem = document.getElementById('modalCompletedCount');
    const successCountElem = document.getElementById('modalSuccessCount');
    const failedCountElem = document.getElementById('modalFailedCount');
    const progressBarElem = document.getElementById('modalProgressBar');
    const statusTextElem = document.getElementById('modalStatusText');
    const cancelBtnElem = document.getElementById('btnCancelSigning');
    const closeBtnElem = document.getElementById('btnCloseSigningModal');
    const closeHeaderBtnElem = document.getElementById('btnCloseSigningModalHeader');

    if (totalCountElem) totalCountElem.textContent = selectedIds.length;
    if (completedCountElem) completedCountElem.textContent = '0';
    if (successCountElem) successCountElem.textContent = '0';
    if (failedCountElem) failedCountElem.textContent = '0';
    if (progressBarElem) {
        progressBarElem.style.width = '0%';
        progressBarElem.textContent = '0%';
    }
    if (statusTextElem) statusTextElem.textContent = 'Đang khởi tạo Job ký...';

    // Show Cancel button, disable Close buttons while signing is active
    if (cancelBtnElem) {
        cancelBtnElem.classList.remove('d-none');
        cancelBtnElem.disabled = false;
    }
    if (closeBtnElem) closeBtnElem.disabled = true;
    if (closeHeaderBtnElem) closeHeaderBtnElem.disabled = true;

    const closeModal = function () {
        if (bsModal) bsModal.hide();
    };
    if (closeBtnElem) closeBtnElem.onclick = closeModal;
    if (closeHeaderBtnElem) closeHeaderBtnElem.onclick = closeModal;

    const itemsTbody = document.getElementById('signingModalItemsTbody');
    if (itemsTbody) {
        itemsTbody.innerHTML = `<tr><td colspan="5" class="text-center text-muted py-3"><div class="spinner-border spinner-border-sm me-2"></div>Đang tải thông tin chi tiết hóa đơn...</td></tr>`;
    }

    bsModal.show();

    // Start Batch Signing Queue
    SigningQueue.startJob(selectedIds, {
        onJobCreated: function (job) {
            renderModalItemsTable(job.items);
            const statusText = document.getElementById('modalStatusText');
            if (statusText) {
                statusText.textContent = `Đang xử lý ký ${job.total} hóa đơn (Sắp xếp theo Ngày xuất ASC)...`;
            }
        },
        onItemStatusChange: function (item, job) {
            updateModalProgressUI(item, job);
        },
        onFinished: function (job, isCompleted, errorCode) {
            const cancelBtn = document.getElementById('btnCancelSigning');
            const closeBtn = document.getElementById('btnCloseSigningModal');
            const closeHeaderBtn = document.getElementById('btnCloseSigningModalHeader');
            const statusText = document.getElementById('modalStatusText');

            if (cancelBtn) cancelBtn.classList.add('d-none');
            if (closeBtn) closeBtn.disabled = false;
            if (closeHeaderBtn) closeHeaderBtn.disabled = false;

            if (statusText) {
                if (isCompleted) {
                    statusText.innerHTML = `<span class="text-success"><i class="bi bi-check-circle-fill me-1"></i> Hoàn thành tiến trình ký! Thành công ${job.success}/${job.total}.</span>`;
                } else if (errorCode === "SIGNER_NOT_RUNNING") {
                    statusText.innerHTML = `<span class="text-danger"><i class="bi bi-x-circle-fill me-1"></i> Lỗi: Ứng dụng WinForms Signer không hoạt động.</span>`;
                } else {
                    statusText.innerHTML = `<span class="text-warning"><i class="bi bi-exclamation-triangle-fill me-1"></i> Tiến trình đã bị hủy. Đã xử lý ${job.completed}/${job.total}.</span>`;
                }
            }

            // Auto refresh main list behind modal
            filterInvoicesAjax();
        }
    });

    if (cancelBtnElem) {
        cancelBtnElem.onclick = function () {
            if (confirm("Bạn có chắc chắn muốn hủy tiến trình ký số không? Hóa đơn đang ký dở sẽ hoàn tất, các hóa đơn chưa ký sẽ bị dừng.")) {
                SigningQueue.cancel();
                this.disabled = true;
                const statusText = document.getElementById('modalStatusText');
                if (statusText) statusText.textContent = "Đang dừng tiến trình ký...";
            }
        };
    }
}

function renderModalItemsTable(items) {
    const tbody = document.getElementById('signingModalItemsTbody');
    if (!tbody || !Array.isArray(items)) return;

    tbody.innerHTML = items.map((item, idx) => {
        const dateStr = item.invoiceDate ? new Date(item.invoiceDate).toLocaleDateString('vi-VN') : '';
        return `
            <tr id="modal-row-${item.invoiceId}">
                <td>${idx + 1}</td>
                <td class="fw-bold">${item.invoiceNumber || ''}</td>
                <td>${dateStr}</td>
                <td><span class="badge bg-secondary item-status-badge">Đang chờ</span></td>
                <td class="item-detail-cell text-muted">-</td>
            </tr>
        `;
    }).join('');
}

function updateModalProgressUI(item, job) {
    const completedElem = document.getElementById('modalCompletedCount');
    const successElem = document.getElementById('modalSuccessCount');
    const failedElem = document.getElementById('modalFailedCount');
    const progressBar = document.getElementById('modalProgressBar');

    if (completedElem) completedElem.textContent = job.completed;
    if (successElem) successElem.textContent = job.success;
    if (failedElem) failedElem.textContent = job.failed;

    if (progressBar && job && job.total > 0) {
        const percent = Math.round((job.completed / job.total) * 100);
        progressBar.style.width = `${percent}%`;
        progressBar.textContent = `${percent}%`;

        // Hide Cancel button when progress bar reaches 100% or job completes
        if (percent >= 100 || job.completed >= job.total) {
            const cancelBtn = document.getElementById('btnCancelSigning');
            const closeBtn = document.getElementById('btnCloseSigningModal');
            const closeHeaderBtn = document.getElementById('btnCloseSigningModalHeader');
            if (cancelBtn) cancelBtn.classList.add('d-none');
            if (closeBtn) closeBtn.disabled = false;
            if (closeHeaderBtn) closeHeaderBtn.disabled = false;
        }
    }

    if (!item) return;

    const row = document.getElementById(`modal-row-${item.invoiceId}`);
    if (row) {
        const badge = row.querySelector('.item-status-badge');
        const detailCell = row.querySelector('.item-detail-cell');

        if (item.status === 'GettingXml') {
            if (badge) {
                badge.className = 'badge bg-info text-dark';
                badge.textContent = 'Đang lấy XML';
            }
        } else if (item.status === 'Signing') {
            if (badge) {
                badge.className = 'badge bg-primary';
                badge.textContent = 'Đang ký WinForms';
            }
        } else if (item.status === 'Submitting') {
            if (badge) {
                badge.className = 'badge bg-warning text-dark';
                badge.textContent = 'Đang cập nhật';
            }
        } else if (item.status === 'Success') {
            if (badge) {
                badge.className = 'badge bg-success';
                badge.textContent = 'Thành công';
            }
            if (detailCell) {
                detailCell.className = 'item-detail-cell text-success';
                detailCell.textContent = 'Ký & Cập nhật thành công';
            }

            // Real-time update in main grid behind modal
            const mainRow = document.querySelector(`.invoice-data-row[data-id="${item.invoiceId}"]`);
            if (mainRow) {
                const cb = mainRow.querySelector('.invoice-checkbox');
                if (cb) {
                    cb.checked = false;
                    cb.disabled = true;
                    cb.title = "Hóa đơn đã được ký số";
                }
                const badgeElem = mainRow.querySelector('.badge');
                if (badgeElem) {
                    badgeElem.className = 'badge bg-success';
                    badgeElem.textContent = 'ĐÃ KÝ SỐ';
                }
                const signLink = mainRow.querySelector('.btn-sign-single');
                if (signLink) {
                    signLink.remove();
                }
            }
        } else if (item.status === 'Failed') {
            if (badge) {
                badge.className = 'badge bg-danger';
                badge.textContent = 'Thất bại';
            }
            if (detailCell) {
                detailCell.className = 'item-detail-cell text-danger';
                detailCell.textContent = item.errorMessage || item.errorCode || 'Lỗi ký';
            }
        }
    }
}

/**
 * Open Invoice Preview Modal (Matching xem-truoc-hoa-don.png)
 * Handles "Không lấy được XML hóa đơn từ server" error state gracefully.
 */
function openInvoicePreviewModal(invoiceId) {
    const container = document.getElementById('invoicePreviewContainer');
    if (!container) return;

    fetch(`/Invoice/GetPreview?id=${invoiceId}`)
        .then(async response => {
            if (response.ok) {
                const html = await response.text();
                container.innerHTML = html;
                const modalElem = document.getElementById('invoicePreviewModal');
                if (modalElem) {
                    const bsModal = new bootstrap.Modal(modalElem);
                    bsModal.show();
                }
            } else {
                let errData = {};
                try { errData = await response.json(); } catch (_) { }
                const errorMsg = errData.message || "Không lấy được XML hóa đơn từ server.";
                alert(`[LỖI] ${errorMsg}\n\nMã hóa đơn #${invoiceId} không thể tải dữ liệu XML từ server backend.`);
            }
        })
        .catch(err => {
            console.error("Lỗi xem trước hóa đơn:", err);
            alert(`[LỖI HỆ THỐNG] Không lấy được XML hóa đơn từ server: ${err.message}`);
        });
}

/**
 * Show Signing Error Details Modal with troubleshooting guide
 */
function showSigningErrorDetails(errorCode, errorMessage) {
    const codeElem = document.getElementById('helpModalErrorCode');
    const msgElem = document.getElementById('helpModalErrorMessage');
    const modalElem = document.getElementById('signingErrorHelpModal');

    if (codeElem) codeElem.textContent = errorCode || 'SIGN_FAILED';
    if (msgElem) msgElem.textContent = errorMessage || 'Lỗi xử lý ký số hóa đơn.';

    if (modalElem) {
        const bsModal = new bootstrap.Modal(modalElem);
        bsModal.show();
    }
}

/**
 * Print Invoice Preview Area
 */
function printInvoicePreview() {
    const printContent = document.getElementById('printableInvoiceArea');
    if (!printContent) {
        window.print();
        return;
    }
    const printWindow = window.open('', '_blank', 'width=900,height=800');
    printWindow.document.write(`
        <html>
            <head>
                <title>In Hóa Đơn Giá Trị Gia Tăng</title>
                <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css">
                <style>
                    body { font-family: 'Times New Roman', Times, serif; padding: 20px; color: #000; }
                    @@media print {
                        body { padding: 0; }
                        .no-print { display: none !important; }
                    }
                </style>
            </head>
            <body>
                ${printContent.outerHTML}
            </body>
        </html>
    `);
    printWindow.document.close();
    printWindow.focus();
    setTimeout(() => {
        printWindow.print();
        printWindow.close();
    }, 500);
}

/**
 * Download simulated PDF invoice
 */
function downloadInvoicePdf() {
    alert("Đang khởi tạo tập tin PDF hóa đơn điện tử chuẩn Nacencomm CA2...\nQuá trình hoàn tất!");
    printInvoicePreview();
}

