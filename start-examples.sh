if [[ "$IN_DEVCONTAINER" == "true" ]]
then
    NB_DIR=/workspaces/qsharp-server/examples
    echo "Opening examples in $NB_DIR..."
    /opt/conda/bin/jupyter lab \
        --notebook-dir=$NB_DIR \
        --ip='*' \
        --port=9888 \
        --no-browser \
        --allow-root
else
    NB_DIR=$(dirname $0)/examples
    echo "Opening examples in $NB_DIR..."
    jupyter lab \
        --notebook-dir=$NB_DIR
fi
