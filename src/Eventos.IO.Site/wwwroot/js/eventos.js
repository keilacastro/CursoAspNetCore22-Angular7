function ValidacoesEvento() {
    $(function () {
        var $gratuito = $("#Gratuito");
        var $online = $("#Online");

        mostrarValor();
        mostrarEnderecoForm();

        $gratuito.click(function () {
            mostrarValor();
        });

        $online.click(function () {
            mostrarEnderecoForm();
        });

        function mostrarValor() {
            if ($gratuito.is(":checked")) {
                $("#Valor").prop("disabled", true);
            } else {
                $("#Valor").prop("disabled", false);
            }
        }

        function mostrarEnderecoForm() {
            if ($online.is(":checked"))
                $("#enderecoForm").hide();
            else
                $("#enderecoForm").show();
        }
    });
}

function AjaxModal() {
    $(document).ready(function () {
        $(function () {
            $.ajaxSetup({ cache: false });

            $("a[data-modal]").on("click",
                function (e) {
                    $("#enderecoModalContent").load(this.href,
                        function () {
                            $("#enderecoModal").modal({
                                keyboard: true
                            }, "show");
                            bindForm(this);
                        });
                    return false;
                });
        });

        function bindForm(dialog) {
            $('form', dialog).submit(function () {
                $.ajax({
                    url: this.action,
                    type: this.method,
                    data: $(this).serialize(),
                    success: function (result) {
                        if (result.success) {
                            $("#enderecoModal").modal("hide");
                            $("#enderecoTarget").load(result.url); // Carrega o resultado HTML para a div demarcada
                        } else {
                            $("#enderecoModalContent").html(result);
                            bindForm(dialog);
                        }
                    }
                });
                return false;
            });
        }
    });
}
