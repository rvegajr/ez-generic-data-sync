#!/bin/zsh

# --------------------------------------------------------------------------
# Ez-Generic-Data-Sync Test Runner
# 
# A comprehensive script to run unit tests and/or functional test harnesses
# with control over which tests to execute.
# --------------------------------------------------------------------------

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[0;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Set default values
RUN_UNIT_TESTS=false
RUN_FUNCTIONAL_TESTS=false
VERBOSE=false
TEST_FILTER=""
PROJECT_ROOT="$(cd "$(dirname "${0}")/../.." && pwd)"
UNIT_TEST_PROJECT="$PROJECT_ROOT/Src/Tests/UnitTests"
FUNCTIONAL_TEST_PROJECT="$PROJECT_ROOT/Src/Tests/TestHarness"

# Function to display usage information
display_usage() {
    echo "Usage: run-tests.sh [options]"
    echo
    echo "Options:"
    echo "  -u, --unit           Run unit tests"
    echo "  -f, --functional     Run functional tests"
    echo "  -a, --all            Run both unit and functional tests"
    echo "  -v, --verbose        Display detailed test output"
    echo "  -h, --help           Display this help message"
    echo "  -t, --test <filter>  Run tests matching the specified filter"
    echo
    echo "Examples:"
    echo "  ./run-tests.sh --unit                # Run all unit tests"
    echo "  ./run-tests.sh --functional          # Run all functional tests"
    echo "  ./run-tests.sh --all                 # Run all tests"
    echo "  ./run-tests.sh -u -t SyncService     # Run unit tests with 'SyncService' in the name"
    echo
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -u|--unit)
            RUN_UNIT_TESTS=true
            shift
            ;;
        -f|--functional)
            RUN_FUNCTIONAL_TESTS=true
            shift
            ;;
        -a|--all)
            RUN_UNIT_TESTS=true
            RUN_FUNCTIONAL_TESTS=true
            shift
            ;;
        -v|--verbose)
            VERBOSE=true
            shift
            ;;
        -t|--test)
            TEST_FILTER="$2"
            shift 2
            ;;
        -h|--help)
            display_usage
            exit 0
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            display_usage
            exit 1
            ;;
    esac
done

# If no test type is specified, show usage and exit
if [[ "$RUN_UNIT_TESTS" == "false" && "$RUN_FUNCTIONAL_TESTS" == "false" ]]; then
    echo -e "${YELLOW}No test type specified.${NC}"
    display_usage
    exit 1
fi

# Function to check if dotnet is installed
check_dotnet() {
    if ! command -v dotnet &> /dev/null; then
        echo -e "${RED}Error: dotnet is not installed or not in the PATH${NC}"
        echo "Please install .NET SDK from https://dotnet.microsoft.com/download"
        exit 1
    fi
}

# Function to run unit tests
run_unit_tests() {
    echo -e "${BLUE}Running unit tests...${NC}"
    
    local test_cmd="dotnet test $UNIT_TEST_PROJECT"
    
    # Add filter if specified
    if [[ -n "$TEST_FILTER" ]]; then
        test_cmd="$test_cmd --filter Name~$TEST_FILTER"
    fi
    
    # Add verbose flag if specified
    if [[ "$VERBOSE" == "true" ]]; then
        test_cmd="$test_cmd -v normal"
    else
        test_cmd="$test_cmd -v minimal"
    fi
    
    echo "Executing: $test_cmd"
    if eval "$test_cmd"; then
        echo -e "${GREEN}✓ Unit tests completed successfully${NC}"
        return 0
    else
        local exit_code=$?
        echo -e "${RED}✗ Unit tests failed with exit code $exit_code${NC}"
        return $exit_code
    fi
}

# Function to run functional tests
run_functional_tests() {
    echo -e "${BLUE}Running functional test harness...${NC}"
    
    # Build the project first
    echo "Building functional test harness..."
    if ! dotnet build "$FUNCTIONAL_TEST_PROJECT"; then
        echo -e "${RED}✗ Failed to build functional test harness${NC}"
        return 1
    fi
    
    local test_cmd="dotnet run --project $FUNCTIONAL_TEST_PROJECT"
    
    # Add integration-test argument if no specific filter is provided
    if [[ -z "$TEST_FILTER" ]]; then
        test_cmd="$test_cmd integration-test"
    else
        test_cmd="$test_cmd integration-test $TEST_FILTER"
    fi
    
    echo "Executing: $test_cmd"
    if eval "$test_cmd"; then
        echo -e "${GREEN}✓ Functional tests completed successfully${NC}"
        return 0
    else
        local exit_code=$?
        echo -e "${RED}✗ Functional tests failed with exit code $exit_code${NC}"
        return $exit_code
    fi
}

# Main execution
check_dotnet

UNIT_EXIT_CODE=0
FUNCTIONAL_EXIT_CODE=0

# Run the tests based on user selection
if [[ "$RUN_UNIT_TESTS" == "true" ]]; then
    run_unit_tests
    UNIT_EXIT_CODE=$?
fi

if [[ "$RUN_FUNCTIONAL_TESTS" == "true" ]]; then
    run_functional_tests
    FUNCTIONAL_EXIT_CODE=$?
fi

# Output summary
echo
echo -e "${BLUE}========== Test Summary ==========${NC}"
if [[ "$RUN_UNIT_TESTS" == "true" ]]; then
    if [[ $UNIT_EXIT_CODE -eq 0 ]]; then
        echo -e "${GREEN}✓ Unit tests: PASSED${NC}"
    else
        echo -e "${RED}✗ Unit tests: FAILED${NC}"
    fi
fi

if [[ "$RUN_FUNCTIONAL_TESTS" == "true" ]]; then
    if [[ $FUNCTIONAL_EXIT_CODE -eq 0 ]]; then
        echo -e "${GREEN}✓ Functional tests: PASSED${NC}"
    else
        echo -e "${RED}✗ Functional tests: FAILED${NC}"
    fi
fi

# Exit with non-zero if any test failed
if [[ $UNIT_EXIT_CODE -ne 0 || $FUNCTIONAL_EXIT_CODE -ne 0 ]]; then
    exit 1
fi

exit 0
